// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Logging;
using osu.Game.Tournament.Models;

namespace osu.Game.Tournament.IPC
{
    public partial class TosuScoreProvider : Component
    {
        private static readonly Uri endpoint = new Uri("ws://127.0.0.1:24050/websocket/v2");

        [Resolved]
        private LadderInfo ladder { get; set; } = null!;

        [Resolved]
        private MatchIPCInfo ipc { get; set; } = null!;

        private CancellationTokenSource? connectionCancellation;
        private int connectionGeneration;

        private bool hasValidTosuScores;
        private int validBeatmapId;
        private double validMultiplier;
        private TosuTeamScores validScores;

        [BackgroundDependencyLoader]
        private void load()
        {
            ipc.FileScore1.BindValueChanged(_ => updateOutputScores());
            ipc.FileScore2.BindValueChanged(_ => updateOutputScores());
            ipc.BeatmapID.BindValueChanged(_ => invalidateTosuScores());
            ladder.CurrentMatch.BindValueChanged(_ => invalidateTosuScores());
            ladder.UseTosuForEZMultiplier.BindValueChanged(enabled => toggleConnection(enabled.NewValue), true);
        }

        private void toggleConnection(bool enabled)
        {
            connectionCancellation?.Cancel();
            connectionCancellation?.Dispose();
            connectionCancellation = null;
            connectionGeneration++;

            invalidateTosuScores();

            if (!enabled)
            {
                ipc.TosuConnectionState.Value = TosuConnectionState.Disabled;
                return;
            }

            ipc.TosuConnectionState.Value = TosuConnectionState.Searching;
            var cancellation = connectionCancellation = new CancellationTokenSource();
            int generation = connectionGeneration;
            Task.Run(() => connectionLoop(cancellation.Token, generation));
        }

        private async Task connectionLoop(CancellationToken cancellationToken, int generation)
        {
            int retryDelay = 1;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using var socket = new ClientWebSocket();
                    await socket.ConnectAsync(endpoint, cancellationToken).ConfigureAwait(false);
                    scheduleForConnection(generation, () => ipc.TosuConnectionState.Value = TosuConnectionState.ConnectedWaitingForClients);
                    retryDelay = 1;
                    await receiveLoop(socket, cancellationToken, generation).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Could not read tournament scores from tosu");
                }

                if (cancellationToken.IsCancellationRequested)
                    break;

                scheduleForConnection(generation, () =>
                {
                    invalidateTosuScores();
                    ipc.TosuConnectionState.Value = TosuConnectionState.Failed;
                });

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(retryDelay), cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                retryDelay = Math.Min(retryDelay * 2, 10);
                scheduleForConnection(generation, () => ipc.TosuConnectionState.Value = TosuConnectionState.Searching);
            }
        }

        private async Task receiveLoop(ClientWebSocket socket, CancellationToken cancellationToken, int generation)
        {
            var buffer = new byte[16 * 1024];

            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                using var message = new MemoryStream();
                WebSocketReceiveResult result;

                do
                {
                    result = await socket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);

                    if (result.MessageType == WebSocketMessageType.Close)
                        return;

                    message.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                if (result.MessageType != WebSocketMessageType.Text)
                    continue;

                string json = Encoding.UTF8.GetString(message.GetBuffer(), 0, checked((int)message.Length));
                scheduleForConnection(generation, () => processMessage(json));
            }
        }

        private void processMessage(string json)
        {
            RoundBeatmap? beatmap = findCurrentBeatmap();

            if (beatmap?.EZMultiplier == null)
            {
                invalidateTosuScores();
                ipc.TosuConnectionState.Value = TosuScoreParser.HasTournamentClients(json)
                    ? TosuConnectionState.Connected
                    : TosuConnectionState.ConnectedWaitingForClients;
                return;
            }

            var result = TosuScoreParser.TryParse(json, beatmap.ID, beatmap.EZMultiplier.Value, out TosuTeamScores scores);

            if (result == TosuScoreParseResult.Valid)
            {
                hasValidTosuScores = true;
                validBeatmapId = beatmap.ID;
                validMultiplier = beatmap.EZMultiplier.Value;
                validScores = scores;
                ipc.TosuConnectionState.Value = TosuConnectionState.Connected;
                updateOutputScores();
                return;
            }

            invalidateTosuScores();
            ipc.TosuConnectionState.Value = TosuScoreParser.HasTournamentClients(json)
                ? TosuConnectionState.Connected
                : TosuConnectionState.ConnectedWaitingForClients;
        }

        private RoundBeatmap? findCurrentBeatmap()
        {
            int beatmapId = ipc.BeatmapID.Value;
            return ladder.CurrentMatch.Value?.Round.Value?.Beatmaps.FirstOrDefault(b => b.ID == beatmapId);
        }

        private void invalidateTosuScores()
        {
            hasValidTosuScores = false;
            updateOutputScores();
        }

        private void updateOutputScores()
        {
            RoundBeatmap? beatmap = findCurrentBeatmap();
            TosuTeamScores scores = TosuScoreRouter.SelectScores(
                ladder.UseTosuForEZMultiplier.Value,
                new TosuTeamScores(ipc.FileScore1.Value, ipc.FileScore2.Value),
                hasValidTosuScores,
                validScores,
                beatmap?.ID ?? 0,
                beatmap?.EZMultiplier,
                validBeatmapId,
                validMultiplier);

            ipc.Score1.Value = scores.Score1;
            ipc.Score2.Value = scores.Score2;
        }

        private void scheduleForConnection(int generation, Action action) => Schedule(() =>
        {
            if (generation == connectionGeneration && ladder.UseTosuForEZMultiplier.Value)
                action();
        });

        protected override void Dispose(bool isDisposing)
        {
            connectionGeneration++;
            connectionCancellation?.Cancel();
            connectionCancellation?.Dispose();
            base.Dispose(isDisposing);
        }
    }
}
