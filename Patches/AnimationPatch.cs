//credits and licenses in the resources folder
using BanMod;
using UnityEngine;

namespace BanMod
{
    public class AnimationPatch : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Sprite[] frames;
        private float fps;
        private int index;
        private float timer;
        private bool loop = true;

        /* =========================
         *  API PUBBLICA
         * ========================= */

        public static AnimationPatch Play(
            GameObject target,
            string imageBaseName,
            int frameCount,
            float fps,
            bool loop = true
        )
        {
            var anim = GetOrCreate(target);
            anim.LoadFrames(imageBaseName, frameCount);
            anim.StartAnimation(fps, loop);
            return anim;
        }

        public static AnimationPatch PlayOnce(
            GameObject target,
            string imageBaseName,
            int frameCount,
            float fps
        )
        {
            return Play(target, imageBaseName, frameCount, fps, false);
        }

        public static void Stop(GameObject target)
        {
            var anim = target.GetComponent<AnimationPatch>();
            if (anim != null)
                Object.Destroy(anim);
        }

        /* =========================
         *  CORE
         * ========================= */

        private static AnimationPatch GetOrCreate(GameObject target)
        {
            var anim = target.GetComponent<AnimationPatch>();
            if (anim == null)
                anim = target.AddComponent<AnimationPatch>();

            anim.spriteRenderer ??= target.GetComponent<SpriteRenderer>()
                                   ?? target.AddComponent<SpriteRenderer>();

            return anim;
        }

        private void LoadFrames(string baseName, int count)
        {
            frames = new Sprite[count];

            for (int i = 0; i < count; i++)
            {
                frames[i] = Utils.LoadSprite(
                    $"BanMod.Resources.image.{baseName}_{i + 1}.png",
                    100f
                );
            }
        }

        private void StartAnimation(float fps, bool loop)
        {
            this.fps = fps;
            this.loop = loop;
            index = 0;
            timer = 0f;

            if (frames.Length > 0)
                spriteRenderer.sprite = frames[0];
        }

        private void Update()
        {
            if (frames == null || frames.Length < 2) return;

            timer += Time.deltaTime;
            if (timer < 1f / fps) return;

            timer = 0f;
            index++;

            if (index >= frames.Length)
            {
                if (!loop) return;
                index = 0;
            }

            spriteRenderer.sprite = frames[index];
        }
    }
}
