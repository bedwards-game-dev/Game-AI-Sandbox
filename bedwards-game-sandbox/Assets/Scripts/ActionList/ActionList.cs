using System;
using System.Collections.Generic;
using UnityEngine;

namespace ActionList
{
    public class ActionList : MonoBehaviour
    {
        // ------------------------ //
        // Singleton Implementation //
        // ------------------------ //
        public static ActionList Instance { get; private set; }
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning("ActionList: Found Multiple instances of a Singleton, please remove extra instances.");
                Destroy(gameObject);
            }
        }

        // ----------------------- //
        // Interpolation Functions //
        // ----------------------- //
        // TODO: support animation curves
        
        // For more functions, check easings.net
        public delegate float Interp(float start, float current, float end);
        public static float Linear(float start, float current, float end)
        {
            float dividend = end - start;
            if (Mathf.Approximately(dividend, 0f)) return 1.0f;
            float x = (current - start) / dividend;
            return Mathf.Clamp01(x);
        }

        public static float EaseInOut(float start, float current, float end)
        {
            float x = Linear(start, current, end);
            return x < 0.5 ? 4 * x * x * x : 1 - Mathf.Pow(-2 * x + 2, 3) / 2;
        }

        public static float EaseOut(float start, float current, float end)
        {
            float x = Linear(start, current, end);
            return 1 - Mathf.Pow(1 - x, 5);
        }

        // ----------------------- //
        // Actions                 //
        // ----------------------- //
        // TODO: Implement Custom Actions that only return the Interp Multiplier.
        // TODO: Break the Base Action Class into polymorphic subclasses: Timer, Interpolation, and Sub-Actions (Move, Scale, Fade, etc.)
        // TODO: Make Callback decorators part of the BaseAction class. And add events for OnFirstFrame and OnCompletion
        // TODO: Add completed state, and change tasks to only be deleted when Action is in Completed State instead of relying on the return value of Update
        // TODO: Support different TimeScales (UI, Combat, Base, etc.), this should reference a different system which should keep track of each.
        
        public abstract class BaseAction
        {
            private float timer; // Time alive
            private float delay; // Time before starting
            private readonly float interpTime; // Duration of Action
            private readonly Interp interpFunc = Linear; // Interpolation Function
            protected readonly GameObject Target; // Object performing the Action

            protected enum ActionState { Waiting, Running }
            protected ActionState State = ActionState.Waiting;

            protected BaseAction(GameObject target, float interpTime, float delay = 0.0f)
            {
                Target = target;
                this.interpTime = Mathf.Max(0f, interpTime);
                this.delay = delay;
            }

            protected BaseAction(GameObject target, float interpTime, Interp interpFunc, float delay = 0.0f)
            {
                Target = target;
                this.interpTime = Mathf.Max(0f, interpTime);
                this.interpFunc = interpFunc ?? Linear;
                this.delay = delay;
            }

            // Get a linear percent done between 0-1
            public float GetPercentDone()
            {
                return interpTime <= 0.0f ? 1.0f : Linear(0, timer, interpTime);
            }

            // Get the result of the interpolation Function based on the actions timer
            protected float GetInterpolateMultiplier()
            {
                return interpTime <= 0.0f ? 1.0f : interpFunc(0.0f, timer, interpTime);
            }

            // Returns true if Interpolation has finished
            public bool Update()
            {
                // If the target GameObject has been deleted, delete this action
                if (Target == null) return true;

                // Check for delay and wait until done
                delay -= Time.deltaTime;
                if (delay > 0.0f)
                {
                    return false;
                }

                // Transition from waiting to running exactly once
                if (State == ActionState.Waiting)
                {
                    FirstFrameUpdate();
                    State = ActionState.Running;
                }

                // Update Timer
                timer += Time.deltaTime;
                if (timer > interpTime) timer = interpTime;

                // Perform the Action
                Act();

                // If done, return true
                return timer >= interpTime;
            }

            // Placeholder function for any first frame updates
            protected virtual void FirstFrameUpdate() { }

            // Abstract Function for action-specific behavior
            protected abstract void Act();
        }

        public class MoveAction : BaseAction
        {
            private Vector3 start;
            private Vector3 end;
            private Vector3 diff;

            public MoveAction(GameObject target, Vector2 destination, float interpTime, float delay = 0) : base(target, interpTime, delay)
            {
                end = destination;
            }

            public MoveAction(GameObject target, Vector2 destination, float interpTime, Interp interpFunc, float delay = 0) : base(target, interpTime, interpFunc, delay)
            {
                end = destination;
            }

            protected override void Act()
            {
                Target.transform.localPosition = start + (diff * GetInterpolateMultiplier());
            }

            protected override void FirstFrameUpdate()
            {
                start = Target.transform.localPosition;
                end.z = start.z;
                diff = end - start;
            }
        }

        public class ScaleAction : BaseAction
        {
            private Vector3 start;
            private Vector3 end;
            private Vector3 diff;

            public ScaleAction(GameObject target, Vector2 end, float interpTime, float delay = 0) : base(target, interpTime, delay)
            {
                this.end = end;
            }

            public ScaleAction(GameObject target, Vector2 end, float interpTime, Interp interpFunc, float delay = 0) : base(target, interpTime, interpFunc, delay)
            {
                this.end = end;
            }

            protected override void Act()
            {
                Target.transform.localScale = start + (diff * GetInterpolateMultiplier());
            }

            protected override void FirstFrameUpdate()
            {
                start = Target.transform.localScale;
                end.z = start.z;
                diff = end - start;
            }
        }

        public class CallbackAction : BaseAction
        {
            private readonly Action fn;
            public CallbackAction(GameObject target, Action fn, float delay) : base(target, 0, delay)
            {
                this.fn = fn;
            }

            protected override void Act()
            {
                fn?.Invoke();
            }
        }

        public class CanvasFadeAction : BaseAction
        {
            private float start;
            private readonly float end;
            private float diff;

            private readonly CanvasGroup canvas;

            public CanvasFadeAction(CanvasGroup target, float end, float interpTime, float delay = 0) : base(target.gameObject, interpTime, delay)
            {
                this.canvas = target;
                this.end = end;
            }

            public CanvasFadeAction(CanvasGroup target, float end, float interpTime, Interp interpFunc, float delay = 0) : base(target.gameObject, interpTime, interpFunc, delay)
            {
                this.canvas = target;
                this.end = end;
            }

            protected override void Act()
            {
                if (canvas.isActiveAndEnabled)
                {
                    canvas.alpha = start + (diff * GetInterpolateMultiplier());
                }
            }

            protected override void FirstFrameUpdate()
            {
                if (!canvas.isActiveAndEnabled) return;
                start = canvas.alpha;
                diff = end - start;
            }
        }

        // ----------------------- //
        // Action List             //
        // ----------------------- //
        // TODO: Add a Remove() Function
        
        // The current list of actions being performed
        private readonly List<BaseAction> currentActions = new();
        // Actions to add to the list at the beginning of every update
        private readonly List<BaseAction> actionsToAdd = new();

        // Update is called once per frame
        private void Update()
        {
            // Add new actions
            if (actionsToAdd.Count > 0)
            {
                currentActions.AddRange(actionsToAdd);
                actionsToAdd.Clear();
            }

            // Update current actions
            // Note: Use in-place compacting to remove finished actions
            int write = 0;
            for (var read = 0; read < currentActions.Count; ++read)
            {
                BaseAction act = currentActions[read];
                if (!act.Update())
                {
                    currentActions[write++] = act;
                }
            }

            // Remove the uncompacted range
            if (write < currentActions.Count)
            {
                currentActions.RemoveRange(write, currentActions.Count - write);
            }
        }

        public void Add(BaseAction act)
        {
            if (act == null) return;
            actionsToAdd.Add(act);
        }

        public void Clear()
        {
            currentActions.Clear();
            actionsToAdd.Clear();
        }
    }
}