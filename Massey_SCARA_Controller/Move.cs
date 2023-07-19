using SCARA_GUI.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SCARA_GUI
{
    public partial class MainWindow : Window
    {
        private bool Validate(string s, int min, int max, out int out_)
        {
            out_ = int.MinValue;
            if (Int32.TryParse(s, out int i))
            {
                if (i <= max && i >= min)
                {
                    out_ = i;
                    return true;
                }
            }
            return false;
        }

        void MovementHandler()
        {
            Ui_UpdateMoveParams();
            PORT_SCARA_Send(pose.Move());
        }
        void MovementHandler(Pose.Axis axis, string val)
        {
            pose.Nudge(axis, val);
            MovementHandler();
        }
        void MovementHandler(Pose.Axis axis, int op, int val = 0)
        {
            pose.Nudge(axis, op, val);
            MovementHandler();
        }

        Pose pose = new Pose(0, 0, 0, Settings.Default);

        internal class Pose
        {
            // VARS AND INITS
            // Fields
            private int W, X, Y;

            // Reference to the settings
            private Settings Settings;

            // Constructor
            public Pose(int w, int x, int y, Settings settings)
            {
                W = w;
                X = x;
                Y = y;
                Settings = settings;
            }

            // Axis and Bound enums
            public enum Axis { W, X, Y }
            private enum Bound { max, min }


            // PUBLIC METHODS
            // Get the values of the Fields
            public int Read(Axis a) { return AxisToInt(a); }

            // Reset the values to HOME
            public void Home()
            {
                W = 0;
                X = 0;
                Y = 0;
            }

            // Get the MOVE command string
            public string Move() { return $"MOVE,{X},{Y},{W}"; }

            // Method to change the value of one of the axies
            /// <summary>
            /// Update the bot's position on axis "axis".
            /// Operation -1 for decrement one, 0 to supply your own value "val", or +1 to increment one
            /// </summary>
            /// <param name="axis"></param>
            /// <param name="operation"></param>
            /// <param name="val"></param>
            /// <returns>
            /// Returns wether anything was changed (a valid selection)
            /// </returns>
            public bool Nudge(Axis axis, int operation, int val = 0)
            {
                Console.WriteLine($"Nudge A:{axis} O: {operation} V: {val}");
                // Check for invalid parameters
                if (operation > 1 || operation < -1) return false;

                // Move to specific coordinate
                int target = operation == 0 ? val : AxisToInt(axis) + operation;

                // Validate target and save
                if (Validate(axis, target))
                {
                    SetValue(axis, target);
                    return true;
                }
                return false;
            }
            public bool Nudge(Axis axis, string val = "") 
            { 
                if (Int32.TryParse(val, out int i)) 
                    return Nudge(axis, 0, i);

                return false;
            }

            // Limits have been updated
            public void LimitsUpdated()
            {
                if      (W > BoundOf(Axis.W, Bound.max)) W = BoundOf(Axis.W, Bound.max);
                else if (W < BoundOf(Axis.W, Bound.min)) W = BoundOf(Axis.W, Bound.min);

                if (X > BoundOf(Axis.X, Bound.max)) X = BoundOf(Axis.X, Bound.max);
                else if (X < BoundOf(Axis.X, Bound.min)) X = BoundOf(Axis.X, Bound.min);

                if (Y > BoundOf(Axis.Y, Bound.max)) Y = BoundOf(Axis.Y, Bound.max);
                else if (Y < BoundOf(Axis.Y, Bound.min)) Y = BoundOf(Axis.Y, Bound.min);
            }

            // PRIVATE METHODS
            // Check that a given value is within bounds
            private bool Validate(Axis axis, int target)
            {
                return target <= BoundOf(axis, Bound.max) && target >= BoundOf(axis, Bound.min);
            }

            // Get the specified Bound of an axis
            private int BoundOf(Axis a, Bound b)
            {
                switch (b)
                {
                    case Bound.max:
                        switch (a)
                        {
                            case Axis.W: return Settings.max_W;
                            case Axis.X: return Settings.max_X;
                            case Axis.Y: return Settings.max_Y;
                            default: throw new ArgumentOutOfRangeException("Invalid argument - must be an enum");
                        }
                    case Bound.min:
                        switch (a)
                        {
                            case Axis.W: return Settings.min_W;
                            case Axis.X: return Settings.min_X;
                            case Axis.Y: return Settings.min_Y;
                            default: throw new ArgumentOutOfRangeException("Invalid argument - must be an enum");
                        }
                    default: throw new ArgumentOutOfRangeException("Invalid argument - must be an enum");
                }
            }

            // Get the current value of an axis
            private int AxisToInt(Axis axis)
            {
                switch (axis)
                {
                    case Axis.X: return X;
                    case Axis.Y: return Y;
                    case Axis.W: return W;
                    default: throw new ArgumentOutOfRangeException("Invalid argument - must be an enum");
                }
            }

            // Set the value of an axis
            private void SetValue(Axis axis, int value)
            {
                switch (axis)
                {
                    case Axis.X:
                        X = value;
                        return;
                    case Axis.Y:
                        Y = value;
                        return;
                    case Axis.W:
                        W = value;
                        return;
                    default:
                        throw new ArgumentOutOfRangeException("Invalid argument - must be an enum");
                }
            }
        }
    }
}
