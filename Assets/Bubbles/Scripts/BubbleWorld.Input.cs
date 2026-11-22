using UnityEngine;
using UnityEngine.InputSystem;

namespace Bubbles
{
    // ポインター入力とジェスチャーの状態遷移を扱う。
    public sealed partial class BubbleWorld
    {
        private void CancelGestures()
        {
            foreach (var p in gestures.Values)
            {
                if (p.created != null)
                {
                    FinishGrowth(p.created);
                }
            }

            gestures.Clear();
            SyncGrowthAudio();
        }

        public Bubble Hit(Vector2 position)
        {
            Bubble found = null;
            foreach (var b in pool)
            {
                if (b.active && !b.growing && (b.position - position).sqrMagnitude < Mathf.Pow(b.radius + 5, 2) && (found == null || b.birthOrder > found.birthOrder))
                {
                    found = b;
                }
            }

            return found;
        }

        public void BeginPointer(int id, Vector2 position, float now)
        {
            if (Paused || settingsUI.ContainsScreenPoint(position / pixelScale))
            {
                return;
            }

            gestures[id] = new Gesture
            {
                start = position,
                previous = position,
                started = now,
                last = now,
                target = Hit(position)
            };
        }

        public void MovePointer(int id, Vector2 position, float now)
        {
            if (!gestures.TryGetValue(id, out var p))
            {
                return;
            }

            p.maxDistance = Mathf.Max(p.maxDistance, Vector2.Distance(position, p.start));
            if (p.maxDistance > tuning.movePixels)
            {
                p.mode = GestureMode.Wind;
                if (p.created != null)
                {
                    FinishGrowth(p.created);
                    p.created = null;
                    SyncGrowthAudio();
                }
            }

            if (p.mode == GestureMode.Wind)
            {
                ApplyWind(p.previous, position, Mathf.Max(now - p.last, .0001f));
            }

            p.previous = position;
            p.last = now;
        }

        public void EndPointer(int id, Vector2 position, float now, bool cancelled = false)
        {
            if (!gestures.TryGetValue(id, out var p))
            {
                return;
            }

            p.maxDistance = Mathf.Max(p.maxDistance, Vector2.Distance(position, p.start));
            if (p.created != null)
            {
                FinishGrowth(p.created);
            }

            if (!cancelled && p.mode == GestureMode.Pending && p.maxDistance <= tuning.movePixels)
            {
                if (p.target != null && p.target.active)
                {
                    if (now - p.started < tuning.tapSeconds)
                    {
                        Tap(p.target);
                    }
                }
                else
                {
                    Spawn(position, tuning.minimumRadius);
                    PlaySound(tapClip, SizeAudioGain(tuning.minimumRadius));
                }
            }

            gestures.Remove(id);
            SyncGrowthAudio();
        }

        private void PollInput()
        {
            float now = Time.unscaledTime;
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                for (int slot = 0; slot < touchscreen.touches.Count; slot++)
                {
                    var t = touchscreen.touches[slot];
                    var p = t.position.ReadValue() * pixelScale;
                    if (t.press.wasPressedThisFrame)
                    {
                        int id = t.touchId.ReadValue();
                        touchSlots[slot] = id;
                        BeginPointer(id, p, now);
                    }
                    else if (t.press.isPressed && touchSlots.TryGetValue(slot, out int id))
                    {
                        MovePointer(id, p, now);
                    }

                    if (t.press.wasReleasedThisFrame && touchSlots.TryGetValue(slot, out int released))
                    {
                        EndPointer(released, p, now, t.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled);
                        touchSlots.Remove(slot);
                    }
                }
            }

            var mouse = Mouse.current;
            if (mouse == null || touchSlots.Count > 0)
            {
                return;
            }

            var mp = mouse.position.ReadValue() * pixelScale;
            if (mouse.leftButton.wasPressedThisFrame)
            {
                BeginPointer(-1, mp, now);
            }
            else if (mouse.leftButton.isPressed)
            {
                MovePointer(-1, mp, now);
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                EndPointer(-1, mp, now);
            }
        }
    }
}
