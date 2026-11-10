using System.Collections.Generic;
using UnityEngine;

namespace Madness.Mods
{
    /// <summary>
    /// Diyalog satırlarını modların değiştirebilmesi için context.
    /// StoryManager veya DialogueBox çağrılarından önce Publish edilebilir.
    /// </summary>
    public sealed class DialogueContext : ModContext
    {
        public string EventId;
        public string SpeakerName;
        public List<DialogueLine> Lines = new List<DialogueLine>();
        public Sprite Portrait;
        public GameObject NpcPrefab;
        public bool CancelDialogue;
    }

    public static class ModDialogue
    {
        /// <summary>
        /// Mevcut diyalog satırlarını modların değiştirmesine izin verir.
        /// </summary>
        public static List<DialogueLine> Apply(string eventId, string speaker, List<DialogueLine> original, Sprite portrait = null, GameObject npcPrefab = null)
        {
            var ctx = new DialogueContext
            {
                EventId = eventId ?? "",
                SpeakerName = speaker ?? "",
                Lines = original != null ? new List<DialogueLine>(original) : new List<DialogueLine>(),
                Portrait = portrait,
                NpcPrefab = npcPrefab
            };
            ModEventBus.Publish(ctx);
            return ctx.CancelDialogue ? new List<DialogueLine>() : ctx.Lines;
        }
    }
}
