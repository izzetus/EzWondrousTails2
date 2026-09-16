using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Controllers;
using KamiToolKit.Enums;
using KamiToolKit.Extensions;
using KamiToolKit.Nodes;

namespace WondrousTailsSolver;

public unsafe class AddonWeeklyBingoController : IAsyncDisposable {
    private readonly AddonController<AddonWeeklyBingo> controller;
    private ushort originalTextNodeHeight;
    private byte[]? originalTextNodeString;
    private BackgroundTextNode? probabilityCardNode;

    public AddonWeeklyBingoController() {
        controller = new AddonController<AddonWeeklyBingo> {
            AddonName = "WeeklyBingo",
            OnSetup = AttachNodes,
            OnRefresh = AddonRefresh,
            OnUpdate = AddonRefresh,
            OnFinalize = DetachNodes,
        };
    }

    public Task EnableAsync() => controller.EnableAsync();

    public ValueTask DisposeAsync() => controller.DisposeAsync();

    private void AttachNodes(AddonWeeklyBingo* addon) {
        var existingTextNode = addon->GetTextNodeById(34);
        if (existingTextNode is null) return;

        originalTextNodeHeight = existingTextNode->GetHeight();

        const float cardSpacing = 2.0f;
        var cardHeight = Math.Clamp(originalTextNodeHeight * 0.48f, 42.0f, 54.0f);
        var messageHeight = Math.Max(20.0f, originalTextNodeHeight - cardHeight - cardSpacing);

        // Keep the game's message above the card while staying inside the original text region.
        existingTextNode->SetHeight((ushort)messageHeight);

        probabilityCardNode = new BackgroundTextNode {
            NodeFlags = NodeFlags.Enabled | NodeFlags.Visible,
            Size = new Vector2(existingTextNode->GetWidth() - 8.0f, cardHeight),
            Position = new Vector2(existingTextNode->GetXFloat() + 4.0f, existingTextNode->GetYFloat() + messageHeight + cardSpacing),
            BackgroundColor = new Vector4(0.02f, 0.02f, 0.02f, 0.88f),
            TextColor = Vector4.One,
            TextOutlineColor = new Vector4(0.0f, 0.0f, 0.0f, 1.0f),
            FontSize = 11,
            FontType = FontType.MiedingerMed,
            TextFlags = TextFlags.MultiLine | TextFlags.Edge,
        };

        probabilityCardNode.TextNode.AlignmentType = AlignmentType.Left;
        probabilityCardNode.TextNode.LineSpacing = 13;

        UpdateProbabilityText();
        probabilityCardNode.AttachNode((AtkResNode*)existingTextNode, NodePosition.AfterTarget);
    }
    
    private void AddonRefresh(AddonWeeklyBingo* addon) {
        foreach (var index in Enumerable.Range(0, 16)) {
            System.PerfectTails.GameState[index] = PlayerState.Instance()->IsWeeklyBingoStickerPlaced(index);
        }

        if (probabilityCardNode is not null) {
            var existingTextNode = addon->GetTextNodeById(34);
            if (existingTextNode is null) return;
            originalTextNodeString ??= SeString.Parse(existingTextNode->NodeText).Encode();
            var nodeText = SeString.Parse(existingTextNode->NodeText);

            var lineBreakIndex = -1;
            for (var index = 0; index < nodeText.Payloads.Count; index++)
            {
                if (index > 0)
                {
                    var previousPayload = nodeText.Payloads[index - 1];
                    var payload = nodeText.Payloads[index];

                    if (previousPayload.Type is PayloadType.NewLine && payload.Type is PayloadType.NewLine)
                    {
                        lineBreakIndex = index - 1;
                        break;
                    }
                }
            }

            if (lineBreakIndex is not -1)
            {
                var newString = new SeStringBuilder();

                for (var index = 0; index < lineBreakIndex; index++)
                {
                    newString.Add(nodeText.Payloads[index]);
                }
                existingTextNode->SetText(newString.Encode());
            }

            UpdateProbabilityText();
        }
    }

    private void UpdateProbabilityText() {
        if (probabilityCardNode is not null) {
            probabilityCardNode.TextNode.Node->SetText(System.PerfectTails.SolveAndGetProbabilitySeString().Encode());
        }
    }

    private void DetachNodes(AddonWeeklyBingo* addon) {
        var existingTextNode = addon->GetTextNodeById(34);
        if (existingTextNode is not null) {
            if (originalTextNodeHeight is not 0) {
                existingTextNode->SetHeight(originalTextNodeHeight);
            }

            if (originalTextNodeString is not null) {
                existingTextNode->SetText(originalTextNodeString);
            }
        }

        probabilityCardNode?.Dispose();
        probabilityCardNode = null;
        originalTextNodeHeight = 0;
        originalTextNodeString = null;
    }
}
