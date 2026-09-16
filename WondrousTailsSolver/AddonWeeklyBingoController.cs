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
    private TextNode? currentLabelNode;
    private TextNode? shuffleLabelNode;
    private TextNode[] currentValueNodes = [];
    private TextNode[] shuffleValueNodes = [];

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
        var cardHeight = Math.Clamp(originalTextNodeHeight * 0.38f, 34.0f, 42.0f);
        var messageHeight = Math.Max(20.0f, originalTextNodeHeight - cardHeight - cardSpacing);
        var cardWidth = Math.Min(existingTextNode->GetWidth() - 16.0f, 304.0f);
        var cardOffsetX = (existingTextNode->GetWidth() - cardWidth) / 2.0f;

        // Keep the game's message above the card while staying inside the original text region.
        existingTextNode->SetHeight((ushort)messageHeight);

        probabilityCardNode = new BackgroundTextNode {
            NodeFlags = NodeFlags.Enabled | NodeFlags.Visible,
            Size = new Vector2(cardWidth, cardHeight),
            Position = new Vector2(existingTextNode->GetXFloat() + cardOffsetX, existingTextNode->GetYFloat() + messageHeight + cardSpacing),
            BackgroundColor = new Vector4(0.02f, 0.02f, 0.02f, 0.82f),
            TextColor = Vector4.One,
            TextOutlineColor = new Vector4(0.0f, 0.0f, 0.0f, 1.0f),
            FontSize = 11,
            FontType = FontType.Axis,
            TextFlags = TextFlags.MultiLine | TextFlags.Edge,
        };

        probabilityCardNode.TextNode.IsVisible = false;

        currentLabelNode = CreateCardTextNode(AlignmentType.Left);
        currentLabelNode.AttachNode(probabilityCardNode);

        shuffleLabelNode = CreateCardTextNode(AlignmentType.Left);
        shuffleLabelNode.AttachNode(probabilityCardNode);

        currentValueNodes = Enumerable.Range(0, 3)
            .Select(_ => CreateCardTextNode(AlignmentType.Center))
            .ToArray();
        shuffleValueNodes = Enumerable.Range(0, 3)
            .Select(_ => CreateCardTextNode(AlignmentType.Center))
            .ToArray();

        foreach (var node in currentValueNodes.Concat(shuffleValueNodes)) {
            node.AttachNode(probabilityCardNode);
        }

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
        if (probabilityCardNode is null || currentLabelNode is null || shuffleLabelNode is null) return;

        var (currentValues, shuffleValues) = System.PerfectTails.SolveAndGetProbabilityDisplay();
        var hasShuffleValues = shuffleValues is not null;
        const float horizontalPadding = 8.0f;
        const float labelWidth = 58.0f;
        const float rowHeight = 16.0f;
        var valueWidth = (probabilityCardNode.Width - (horizontalPadding * 2.0f) - labelWidth) / 3.0f;
        var firstRowY = hasShuffleValues ? 3.0f : (probabilityCardNode.Height - rowHeight) / 2.0f;
        var secondRowY = firstRowY + rowHeight;

        currentLabelNode.Position = new Vector2(horizontalPadding, firstRowY);
        currentLabelNode.Size = new Vector2(labelWidth, rowHeight);
        currentLabelNode.Node->SetText("Current");

        shuffleLabelNode.Position = new Vector2(horizontalPadding, secondRowY);
        shuffleLabelNode.Size = new Vector2(labelWidth, rowHeight);
        shuffleLabelNode.Node->SetText("Shuffle");
        shuffleLabelNode.IsVisible = hasShuffleValues;

        for (var index = 0; index < currentValueNodes.Length; index++) {
            var x = horizontalPadding + labelWidth + (valueWidth * index);
            currentValueNodes[index].Position = new Vector2(x, firstRowY);
            currentValueNodes[index].Size = new Vector2(valueWidth, rowHeight);
            currentValueNodes[index].Node->SetText(currentValues[index].Encode());

            shuffleValueNodes[index].Position = new Vector2(x, secondRowY);
            shuffleValueNodes[index].Size = new Vector2(valueWidth, rowHeight);
            shuffleValueNodes[index].IsVisible = hasShuffleValues;
            if (shuffleValues is not null) {
                shuffleValueNodes[index].Node->SetText(shuffleValues[index].Encode());
            }
        }
    }

    private static TextNode CreateCardTextNode(AlignmentType alignment)
        => new() {
            NodeFlags = NodeFlags.Enabled | NodeFlags.Visible,
            TextColor = Vector4.One,
            TextOutlineColor = new Vector4(0.0f, 0.0f, 0.0f, 1.0f),
            FontSize = 11,
            FontType = FontType.Axis,
            AlignmentType = alignment,
            TextFlags = TextFlags.Edge,
        };

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

        foreach (var node in currentValueNodes.Concat(shuffleValueNodes)) {
            node.Dispose();
        }

        currentLabelNode?.Dispose();
        shuffleLabelNode?.Dispose();
        probabilityCardNode?.Dispose();
        probabilityCardNode = null;
        currentLabelNode = null;
        shuffleLabelNode = null;
        currentValueNodes = [];
        shuffleValueNodes = [];
        originalTextNodeHeight = 0;
        originalTextNodeString = null;
    }
}
