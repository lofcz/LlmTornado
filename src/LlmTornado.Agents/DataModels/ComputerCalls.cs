using System.Drawing;

namespace LlmTornado.Agents
{
    /// <summary>
    /// Represents a method that will handle actions performed on a computer tool.
    /// </summary>
    /// <param name="computerCall">The action to be performed on the computer tool.</param>
    public delegate void ComputerActionCallbacks(ComputerToolAction computerCall);

    /// <summary>
    /// Possible Computer Actions
    /// </summary>
    public enum ModelComputerCallAction
    {
        Click,
        DoubleClick,
        Drag,
        KeyPress,
        Move,
        Screenshot,
        Scroll,
        Type,
        Wait,
        Unknown
    }

    /// <summary>
    /// Mouse Buttons
    /// </summary>
    public enum MouseButtons { Right, Left, Middle, Back, Forward }

    /// <summary>
    /// Base Computer Action Class To hold all tool information
    /// </summary>
    public class ComputerToolAction
    {
        /// <summary>
        /// What kind of Action is being performed
        /// </summary>
        public ModelComputerCallAction Kind { get; set; } = ModelComputerCallAction.Unknown;
        /// <summary>
        /// Mouse Coordinates to move to
        /// </summary>
        public Point MoveCoordinates { get; set; }
        /// <summary>
        /// Mouse Button being clicked
        /// </summary>
        public MouseButtons MouseButtonClick { get; set; }
        /// <summary>
        /// Triggers double click to be called on click event
        /// </summary>
        public bool WasDoubleClick { get; set; } = false;
        /// <summary>
        /// Holds <StartX, StartY> <EndX, EndY> Points
        /// </summary>
        public Point StartDragLocation { get; set; }
        /// <summary>
        /// List of keys to press
        /// </summary>
        public List<string> KeysToPress { get; set; }
        /// <summary>
        /// Text to type
        /// </summary>
        public string TypeText { get; set; }
        /// <summary>
        /// Horizontal Scroll amount
        /// </summary>
        public int ScrollHorOffset { get; set; }
        /// <summary>
        /// Vertical Scroll amount
        /// </summary>
        public int ScrollVertOffset { get; set; }
    }

    /// <summary>
    /// Computer Double Click Action
    /// </summary>
    public class ComputerToolActionDoubleClick : ComputerToolAction
    {
        public ComputerToolActionDoubleClick(int toX, int toY)
        {
            Kind = ModelComputerCallAction.DoubleClick;
            WasDoubleClick = true;
            MouseButtonClick = MouseButtons.Left;
            MoveCoordinates = new Point(toX, toY);
        }
    }

    /// <summary>
    /// Computer Click Action
    /// </summary>
    public class ComputerToolActionClick : ComputerToolAction
    {
        public ComputerToolActionClick(int toX, int toY, MouseButtons button)
        {
            Kind = ModelComputerCallAction.Click;
            WasDoubleClick = false;
            MouseButtonClick = button;
            MoveCoordinates = new Point(toX, toY);
        }
    }

    /// <summary>
    /// Computer Dragging Action 
    /// </summary>
    public class ComputerToolActionDrag: ComputerToolAction
    {
        public ComputerToolActionDrag(int fromX, int fromY, int toX, int toY)
        {
            Kind = ModelComputerCallAction.Drag;
            StartDragLocation = new Point(fromX, fromY);
            MoveCoordinates = new Point(toX, toY);
        }
    }

    /// <summary>
    /// Computer Move mouse action
    /// </summary>
    public class ComputerToolActionMove : ComputerToolAction
    {
        public ComputerToolActionMove(int toX, int toY)
        {
            Kind = ModelComputerCallAction.Move;
            MoveCoordinates = new Point(toX, toY);
        }
    }

    /// <summary>
    /// Computer Press Key action
    /// </summary>
    public class ComputerToolActionKeyPress : ComputerToolAction
    {
        public ComputerToolActionKeyPress(List<string>? keys)
        {
            Kind = ModelComputerCallAction.KeyPress;
            KeysToPress = keys ?? new List<string>();
        }
    }

    /// <summary>
    /// Computer Typing Action
    /// </summary>
    public class ComputerToolActionType : ComputerToolAction
    {
        public ComputerToolActionType(string text)
        {
            Kind = ModelComputerCallAction.Type;
            TypeText = text;
        }
    }

    /// <summary>
    /// Computer Waiting Action
    /// </summary>
    public class ComputerToolActionWait : ComputerToolAction
    {
        public ComputerToolActionWait()
        {
            Kind = ModelComputerCallAction.Wait;
        }
    }

    /// <summary>
    /// Computer Screen Shot action
    /// </summary>
    public class ComputerToolActionScreenShot : ComputerToolAction
    {
        public ComputerToolActionScreenShot()
        {
            Kind = ModelComputerCallAction.Screenshot;
        }
    }

    /// <summary>
    /// Computer Scroll Action
    /// </summary>
    public class ComputerToolActionScroll: ComputerToolAction
    {
        public ComputerToolActionScroll(int offsetVertical = 0, int offsetHorizontal = 0)
        {
            Kind = ModelComputerCallAction.Scroll;
            ScrollHorOffset = offsetHorizontal;
            ScrollVertOffset = offsetVertical;
        }
    }

    /// <summary>
    /// Computer Call Input Item
    /// </summary>
    public class ModelComputerCallItem : CallItem
    {
        /// <summary>
        /// Status of the computer call (always complete)
        /// </summary>
        public ModelStatus Status { get; set; }
        
        /// <summary>
        /// Computer Action being called
        /// </summary>
        public ComputerToolAction Action { get; set; }

        public ModelComputerCallItem(string id, string callId, ModelStatus status, ComputerToolAction action) : base(id, callId)
        {
            Id = id;
            Status = status;
            Action = action;
            CallId = callId;
        }
    }

    /// <summary>
    /// Computer Call output item.. this is automatically created after Callback is Invoked
    /// </summary>
    public class ModelComputerCallOutputItem : CallItem
    {
        /// <summary>
        /// Status of the computer output Item (always complete)
        /// </summary>
        public ModelStatus Status { get; set; }
        /// <summary>
        /// Image of the screen
        /// </summary>
        public ModelMessageImageFileContent ScreenShot { get; set; }
        public ModelComputerCallOutputItem(string id, string callId,  ModelStatus status, ModelMessageImageFileContent screenShot) : base(id, callId)
        {
            Id = id;
            Status = status;
            CallId = callId;
            ScreenShot = screenShot;
        }
    }

}
