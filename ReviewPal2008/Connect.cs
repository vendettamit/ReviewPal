using System;
using Extensibility;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.CommandBars;
using System.Reflection;
using ReviewPal.Core;
using ReviewPal.UI;
namespace ReviewPal
{
    /// <summary>The object for implementing an Add-in.</summary>
    /// <seealso class='IDTExtensibility2' />
    public class Connect : IDTExtensibility2, IDTCommandTarget
    {
        private DTE2 _visualStudioInstance;
        private AddIn _addInInstance;
        private static Window _windowToolWindow;

        /// <summary>Implements the constructor for the Add-in object. Place your initialization code within this method.</summary>
        public Connect()
        {
        }

        /// <summary>Implements the OnConnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being loaded.</summary>
        /// <param term='application'>Root object of the host application.</param>
        /// <param term='connectMode'>Describes how the Add-in is being loaded.</param>
        /// <param term='addInInst'>Object representing this Add-in.</param>
        /// <param name="application"></param>
        /// <param name="connectMode"></param>
        /// <param name="addInInst"></param>
        /// <param name="custom"></param>
        /// <seealso class='IDTExtensibility2' />
        public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
        {
            _visualStudioInstance = (DTE2)application;
            _addInInstance = (AddIn)addInInst;

            try
            {
                if (connectMode == ext_ConnectMode.ext_cm_AfterStartup)
                {
                    SetupReviewPalMenuItem();
                    InitializePlugIn();
                }
            }
            catch (Exception ex)
            {
                Utils.HandleException(ex);
            }
        }

        /// <summary>
        /// Initializes the plug in.
        /// </summary>
        private void InitializePlugIn()
        {
            object programmableObject = null;
            String guidstr = "{858C3FCD-8B39-4540-A592-F31C1520B174}";
            Windows2 windows2 = (EnvDTE80.Windows2)_visualStudioInstance.Windows;
            Assembly asm = Assembly.GetExecutingAssembly();
                
            Utils.AssemblyTitle = GetAssemblyTitle(asm);
            Utils.AssemblyPath = GetAssemblyPath(asm);

            _windowToolWindow = windows2.CreateToolWindow2(_addInInstance, asm.Location, "ReviewPal.UI.ReviewWindow",
                                                           "Review List", guidstr, ref programmableObject);

            _windowToolWindow.Visible = true;
            ReviewWindow rCommentControl = (ReviewWindow)_windowToolWindow.Object;
            rCommentControl.VisualStudioInstance = _visualStudioInstance;
        }

        /// <summary>
        /// Setups the reviewPal menu item.
        /// </summary>
        private void SetupReviewPalMenuItem()
        {
            const string toolsMenuName = "Tools";

            //Place the command on the tools menu.
            //Find the MenuBar command bar, which is the top-level command bar holding all the main menu items:
            CommandBar menuBarCommandBar =
                ((CommandBars)_visualStudioInstance.CommandBars)["MenuBar"];

            //Find the Tools command bar on the MenuBar command bar:
            CommandBarControl toolsControl = menuBarCommandBar.Controls[toolsMenuName];
            CommandBarPopup toolsPopup = (CommandBarPopup)toolsControl;

            CommandBar commandBar = toolsPopup.GetType().InvokeMember("CommandBar",
                        BindingFlags.Instance | BindingFlags.GetProperty,
                        null, toolsPopup, null) as CommandBar;

            //This try/catch block can be duplicated if you wish to add multiple commands to be handled by your Add-in,
            //  just make sure you also update the QueryStatus/Exec method to include the new command names.
            try
            {
                AddCommand("ReviewPal", "Review Pal", "Opens up the ReviewPal review list", 1, commandBar, 1, false, null);

            }
            catch (ArgumentException ex)
            {
                //If we are here, then the exception is probably because a command with that name
                //  already exists. If so there is no need to recreate the command and we can 
                //  safely ignore the exception.
            }
        }

        /// <summary>
        /// Adds a new Command and creates a new CommandBar control both of which
        /// can be returned via the AddCommandReturn object that holds refs to both.
        /// </summary>
        /// <param name="name">The name of the Command. Must be handled in the Addin</param>
        /// <param name="caption">The Caption</param>
        /// <param name="description">Tooltip Text</param>
        /// <param name="iconId">Icon Id if you use a custom icon. Otherwise use 0</param>
        /// <param name="commandBar">The Command bar that this command will attach to</param>
        /// <param name="insertionIndex">The InsertionIndex for this CommandBar</param>
        /// <param name="beginGroup">Are we starting a new group on the toolbar (above)</param>
        /// <param name="hotKey">Optional hotkey. Format: "Global::alt+f1"</param>
        /// <returns>AddCommandReturn object that contains a Command and CommandBarControl object that were created</returns>
        private void AddCommand(string name, string caption, string description,
              int iconId, CommandBar commandBar, int insertionIndex,
              bool beginGroup, string hotKey)
        {
            object[] contextGuids = new object[] { };

            // *** Check to see if the Command exists already to be safe
            string commandName = _addInInstance.ProgID + "." + name;
            Command command = null;
            try
            {
                command = _visualStudioInstance.Commands.Item(commandName, -1);
            }
            catch { ;}
            // *** If not create it!
            if (command == null)
            {
                Commands2 commands = (Commands2)_visualStudioInstance.Commands;

                //Add a command to the Commands collection:
                //loading the Custom image icon
                //http://tech.einaregilsson.com/2009/11/20/easy-way-to-have-custom-icons-in-visual-studio-addin/
                command = commands.AddNamedCommand2(_addInInstance,
                        name, caption, description,
                        false, iconId, ref contextGuids,
                        (int)vsCommandStatus.vsCommandStatusSupported + (int)vsCommandStatus.vsCommandStatusEnabled,
                        (int)vsCommandStyle.vsCommandStylePictAndText, vsCommandControlType.vsCommandControlTypeButton);
            }
            CommandBarControl cb = (CommandBarControl)command.AddControl(commandBar, insertionIndex);
            cb.BeginGroup = beginGroup;
        }

        /// <summary>
        /// Removes the command from the visual studio menu's given the command string
        /// </summary>
        /// <param name="Command"></param>
        private void RemoveCommand(string Command)
        {
            Command cmd = null;
            try
            {
                cmd = _visualStudioInstance.Commands.Item(Command, -1);
            }
            catch { ;}
            if (cmd != null)
                cmd.Delete();
        }

        private string GetAssemblyTitle(Assembly asm)
        {
            object[] attributes = asm.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
            if (attributes.Length > 0)
            {
                AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                if (titleAttribute.Title != "")
                {
                    return titleAttribute.Title;
                }
            }
            return "ReviewPal";
        }

        /// <summary>
        /// Gets the assembly path.
        /// </summary>
        /// <param name="asm">The asm.</param>
        /// <returns></returns>
        private string GetAssemblyPath(Assembly asm)
        {
            return System.IO.Path.GetDirectoryName(asm.CodeBase);
        }

        /// <summary>Implements the OnDisconnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being unloaded.</summary>
        /// <param term='disconnectMode'>Describes how the Add-in is being unloaded.</param>
        /// <param term='custom'>Array of parameters that are host application specific.</param>
        /// <seealso class='IDTExtensibility2' />
        public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom)
        {
            try
            {
                //another try to fix the issue of windows status not being saved
                if (_windowToolWindow != null)
                {
                    _windowToolWindow.Close(vsSaveChanges.vsSaveChangesYes);
                }
                if (_windowToolWindow != null && _windowToolWindow.Visible)
                {
                    _windowToolWindow.Visible = false;
                }
                RemoveCommand("ReviewPal.Connect.ReviewPal");
            }
            catch (Exception ex)
            {
                Utils.HandleException(ex);
            }
        }

        /// <summary>Implements the OnAddInsUpdate method of the IDTExtensibility2 interface. Receives notification when the collection of Add-ins has changed.</summary>
        /// <param term='custom'>Array of parameters that are host application specific.</param>
        /// <param name="custom"></param>
        /// <seealso class='IDTExtensibility2' />		
        public void OnAddInsUpdate(ref Array custom)
        {
        }

        /// <summary>Implements the OnStartupComplete method of the IDTExtensibility2 interface. Receives notification that the host application has completed loading.</summary>
        /// <param term='custom'>Array of parameters that are host application specific.</param>
        /// <param name="custom"></param>
        /// <seealso class='IDTExtensibility2' />
        public void OnStartupComplete(ref Array custom)
        {
            try
            {
                SetupReviewPalMenuItem();
                InitializePlugIn();
            }
            catch (Exception ex)
            {
                Utils.HandleException(ex);
            }
        }

        /// <summary>Implements the OnBeginShutdown method of the IDTExtensibility2 interface. Receives notification that the host application is being unloaded.</summary>
        /// <param term='custom'>Array of parameters that are host application specific.</param>
        /// <param name="custom"></param>
        /// <seealso class='IDTExtensibility2' />
        public void OnBeginShutdown(ref Array custom)
        {
        }

        /// <summary>Implements the QueryStatus method of the IDTCommandTarget interface. This is called when the command's availability is updated</summary>
        /// <param term='commandName'>The name of the command to determine state for.</param>
        /// <param term='neededText'>Text that is needed for the command.</param>
        /// <param term='status'>The state of the command in the user interface.</param>
        /// <param term='commandText'>Text requested by the neededText parameter.</param>
        /// <seealso class='Exec' />
        public void QueryStatus(string commandName, vsCommandStatusTextWanted neededText, ref vsCommandStatus status, ref object commandText)
        {
            if (neededText == vsCommandStatusTextWanted.vsCommandStatusTextWantedNone)
            {
                if (commandName == "ReviewPal.Connect.ReviewPal")
                {
                    status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
                    return;
                }
                else
                {
                    status = vsCommandStatus.vsCommandStatusUnsupported;
                    return;
                }
            }
        }

        /// <summary>Implements the Exec method of the IDTCommandTarget interface. This is called when the command is invoked.</summary>
        /// <param term='commandName'>The name of the command to execute.</param>
        /// <param term='executeOption'>Describes how the command should be run.</param>
        /// <param term='varIn'>Parameters passed from the caller to the command handler.</param>
        /// <param term='varOut'>Parameters passed from the command handler to the caller.</param>
        /// <param term='handled'>Informs the caller if the command was handled or not.</param>
        /// <param name="commandName"></param>
        /// <param name="executeOption"></param>
        /// <param name="varIn"></param>
        /// <param name="varOut"></param>
        /// <param name="handled"></param>
        /// <seealso class='Exec' />
        public void Exec(string commandName, vsCommandExecOption executeOption, ref object varIn, ref object varOut, ref bool handled)
        {
            handled = false;
            if (executeOption == vsCommandExecOption.vsCommandExecOptionDoDefault)
            {
                if (commandName == "ReviewPal.Connect.ReviewPal")
                {
                    handled = true;
                    if (_windowToolWindow != null && !_windowToolWindow.Visible)
                    {
                        _windowToolWindow.Visible = true;
                    }
                    return;
                }
            }
        }
    }
}