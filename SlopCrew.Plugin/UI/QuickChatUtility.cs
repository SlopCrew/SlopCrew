using DG.Tweening;
using Microsoft.Extensions.DependencyInjection;
using SlopCrew.Common;
using SlopCrew.Common.Proto;
using TMPro;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace SlopCrew.Plugin.UI;

public static class QuickChatUtility {
    private const float QuickChatStartHeightFactor = 1f;
    private const float QuickChatRestHeightFactor = 1.5f;
    private const float QuickChatEndHeightFactor = 2f;

    private const float QuickChatIntroDuration = 0.5f;
    private const float QuickChatRestDuration = 3f;
    private const float QuickChatOutroDuration = 0.25f;

    public static void SpawnQuickChat(Reptile.Player player, QuickChatCategory category, int index) {
        if (!Constants.QuickChatMessages.TryGetValue(category, out var messages)) return;
        if (index >= messages.Count) return;

        var message = messages[index];
        var interfaceUtility = Plugin.Host.Services.GetRequiredService<InterfaceUtility>();

        var quickChat = new GameObject("SlopCrewQuickChat");
        quickChat.AddComponent<UIBillboard>();

        var tmp = quickChat.AddComponent<TextMeshPro>();
        tmp.text = message;
        tmp.font = interfaceUtility.QuickChatFont;
        tmp.alignment = TextAlignmentOptions.Midline;
        tmp.fontSize = 2.5f;
        tmp.color = new Color(1, 1, 1, 0);
        tmp.spriteAsset = interfaceUtility.EmojiAsset;

        var capsule = player.interactionCollider as CapsuleCollider;
        var height = capsule!.height;

        var startPos = Vector3.up * (height * QuickChatStartHeightFactor);
        var restPos = Vector3.up * (height * QuickChatRestHeightFactor);
        var endPos = Vector3.up * (height * QuickChatEndHeightFactor);

        quickChat.transform.SetParent(capsule.transform, false);
        quickChat.transform.localPosition = startPos;

        var sequence = DOTween.Sequence();

        // Fade the text in and slide it up a bit
        sequence.Append(tmp.DOFade(1, QuickChatIntroDuration));
        var moveInTween = quickChat.transform.DOLocalMoveY(restPos.y, QuickChatIntroDuration);
        moveInTween.SetEase(Ease.OutCubic);
        sequence.Join(moveInTween);

        // Keep it there for a few seconds
        sequence.AppendInterval(QuickChatRestDuration);

        // Fade the text out and slide it up a bit
        sequence.Append(tmp.DOFade(0, QuickChatOutroDuration));
        var moveOutTween = quickChat.transform.DOLocalMoveY(endPos.y, QuickChatOutroDuration);
        moveOutTween.SetEase(Ease.InCubic);
        sequence.Join(moveOutTween);

        // Destroy the object when the animation is done
        sequence.OnComplete(() => Object.Destroy(quickChat));
    }
}
