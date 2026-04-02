using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;


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
    
    private static float Linear(float start, float current, float end)
    {
        float dividend = end - start;
        if (Mathf.Approximately(dividend, 0f)) return 1.0f;
        float x = (current - start) / dividend;
        return Mathf.Clamp01(x);
    }

    // ----------------------- //
    // Actions                 //
    // ----------------------- //
    // TODO: Scale Animation Curves to last for InterpTime 
    // TODO: Break the Base Action Class into polymorphic subclasses: Timer, Interpolation, and Sub-Actions (Move, Scale, Fade, etc.)
    // TODO: Support different TimeScales (UI, Combat, Base, etc.), this should reference a different system which should keep track of each.
    // TODO: Make Actions handle high dt (fallthrough the switch case and track remaining dt)
    
    public class BaseAction
    {
        private float timer; // Time alive
        private float delay; // Time before starting

        private readonly float interpTime; // Duration of Action

        //private readonly Interp interpFunc = Linear; // Interpolation Function
        private readonly AnimationCurve easingCurve; // Interpolation Function
        protected readonly GameObject Target; // Object performing the Action

        // Event Functions
        private event Action OnCompleted;
        private event Action OnStart;
        
        public enum ActionState
        {
            Waiting,
            Running,
            Completed
        }

        protected ActionState State = ActionState.Waiting;

        // Constructor
        protected BaseAction(GameObject target, float interpTime, float delay = 0.0f)
        {
            Target = target;
            easingCurve = AnimationCurve.Linear(0, 0, 1, 1);
            this.interpTime = Mathf.Max(0f, interpTime);
            this.delay = delay;
        }
        
        // Override Constructor for Custom Easing Functions
        protected BaseAction(GameObject target, float interpTime, AnimationCurve easingCurve, float delay = 0.0f)
        {
            Target = target;
            this.interpTime = Mathf.Max(0f, interpTime);
            this.easingCurve = easingCurve ?? AnimationCurve.Linear(0, 0, 1, 1);
            this.delay = delay;
        }

        // Returns the current state of the Action
        public ActionState GetCurrentState() 
        {
            return State;
        }

        // Get a linear percent done between 0-1
        public float GetPercentDone()
        {
            return Linear(0, timer, interpTime);
        }

        // Get the result of the interpolation Function based on the actions timer
        public float GetInterpolateMultiplier()
        {
            return easingCurve.Evaluate(Linear(0, timer, interpTime));
        }
        
        // Add Event Callbacks
        public void AddEventOnCompleted(Action fn)
        {
            OnCompleted += fn;
        }
        public void AddEventOnStart(Action fn)
        {
            OnStart += fn;
        }

        // Returns true if Interpolation has finished
        public void Update()
        {
            // If we're done, stop updating and wait to be cleared
            if (State == ActionState.Completed) return;
            // If the target GameObject has been deleted, delete this action
            if (Target == null)
            {
                Complete(false);
                return;
            }
            
            switch (State)
            {
                case ActionState.Waiting:
                    // Check for delay and wait until done
                    delay -= Time.deltaTime;
                    if (delay > 0.0f) return;
                    // Start will change the current State, as well as call any first frame updates
                    Start();
                    break;
                case ActionState.Running:
                    // Update Timer
                    timer += Time.deltaTime;
                    if (timer > interpTime) timer = interpTime;
                    // Perform the Action
                    Act();
                    // Check if we're done
                    if (!(timer >= interpTime)) return;
                    // Complete the action, running any final updates and callbacks
                    Complete();
                    break;
                case ActionState.Completed:
                default:
                    return;
            }
        }
        private void Start(bool invokeCallbacks = true)
        {
            // Make sure we aren't invoking the start callbacks twice
            if (State == ActionState.Running) return; 

            State = ActionState.Running;
            FirstFrameUpdate();
            if (invokeCallbacks) OnStart?.Invoke();
        }
        private void Complete(bool invokeCallbacks = true)
        {
            // Make sure we aren't invoking the completed callbacks twice
            if (State == ActionState.Completed) return; 
            
            State = ActionState.Completed;
            LastFrameCleanup();
            if (invokeCallbacks) OnCompleted?.Invoke();
        }

        // Placeholder function for any first frame updates
        protected virtual void FirstFrameUpdate() { }
        // Run any cleanup after the last frame of Act
        protected virtual void LastFrameCleanup() { }
        // Abstract Function for action-specific behavior
        protected virtual void Act() { }
        
        // A function for ending the action prematurely
        public void Interrupt(bool invokeCallbacks = true)
        {
            Complete(invokeCallbacks);
        }
    }

    public class MoveAction : BaseAction
    {
        private Vector3 start;
        private Vector3 end;
        private Vector3 diff;

        public MoveAction(GameObject target, Vector3 destination, float interpTime, float delay = 0) : base(target, interpTime, delay)
        {
            end = destination;
        }

        public MoveAction(GameObject target, Vector3 destination, float interpTime, AnimationCurve easingCurve, float delay = 0) : base(target, interpTime, easingCurve, delay)
        {
            end = destination;
        }

        protected override void Act()
        {
            Target.transform.localPosition = start + (diff * GetInterpolateMultiplier());
        }

        protected override void FirstFrameUpdate()
        {
            // Maintain 2D z-value
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

        public ScaleAction(GameObject target, Vector3 end, float interpTime, float delay = 0) : base(target, interpTime, delay)
        {
            this.end = end;
        }

        public ScaleAction(GameObject target, Vector3 end, float interpTime, AnimationCurve easingCurve, float delay = 0) : base(target, interpTime, easingCurve, delay)
        {
            this.end = end;
        }

        protected override void Act()
        {
            Target.transform.localScale = start + (diff * GetInterpolateMultiplier());
        }

        protected override void FirstFrameUpdate()
        {
            // Maintain 2D z-value
            start = Target.transform.localScale;
            end.z = start.z;
            diff = end - start;
        }
    }

    public class CustomAction : BaseAction
    {
        private readonly Action fn;
        public CustomAction(GameObject target, Action fn, float interpTime, float delay = 0) : base(target, interpTime, delay)
        {
            this.fn = fn;
        }
        
        public CustomAction(GameObject target, Action fn, float interpTime, AnimationCurve easingCurve, float delay = 0) : base(target, interpTime, easingCurve, delay)
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

        public CanvasFadeAction(CanvasGroup target, float end, float interpTime, AnimationCurve easingCurve, float delay = 0) : base(target.gameObject, interpTime, easingCurve, delay)
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
    // TODO: Change Debug Output to show delay when delaying
    
    // The current list of actions being performed
    private readonly List<BaseAction> currentActions = new();
    // Actions to add to the list at the beginning of every update
    private readonly List<BaseAction> actionsToAdd = new();
    
    // Debug Info
    [SerializeField] private InputAction debugToggle;
    [SerializeField] private TMP_Text debugTextbox;
    private bool debugPossible = true;
    private bool debugEnabled;
    private string debugOutput = "";
    
    private void Start()
    {
        if (debugToggle == null)
        {
            Debug.LogWarning("ActionList: Debug Toggle not set, cannot show debug info."); 
            debugPossible = false;
        }
        // ReSharper disable once InvertIf for readability
        if (debugTextbox == null)
        {
            Debug.LogWarning("ActionList: Debug Textbox not set, cannot show debug info."); 
            debugPossible = false;
            return;
        }
        debugTextbox.text = "";
    }
    // Update is called once per frame
    private void Update()
    {
        // Check if we should show debug info
        if (debugToggle.triggered) ToggleDebugInfo();
        debugOutput = "Action List:\n";
        
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
            act.Update();
            
            // If the action is done, don't write it back into the action list
            if (act.GetCurrentState() == BaseAction.ActionState.Completed) continue;
            
            // Write the action to the list
            currentActions[write++] = act;
            
            // Add Debug Info
            if (debugEnabled)
            {
                debugOutput += act + " " + act.GetCurrentState() + " " + act.GetPercentDone() + "%\n";
            }
        }

        // Remove the uncompacted range
        if (write < currentActions.Count)
        {
            currentActions.RemoveRange(write, currentActions.Count - write);
        }
        
        // Write the Debug info to the textbox
        if (debugEnabled)
        {
            debugTextbox.text = debugOutput;
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
    
    private void ToggleDebugInfo()
    {
        if (!debugPossible) return;
        
        debugEnabled = !debugEnabled;
        
        // Clear Textbox on disabled
        if (!debugEnabled)
        {
            debugTextbox.text = "";
        }
    }
    
}
