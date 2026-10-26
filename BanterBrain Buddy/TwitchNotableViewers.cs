using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TwitchLib.Api.Helix.Models.Extensions.ReleasedExtensions;

//alright here we add, edit and remove specific MyTwitchNotableViewers. When added you can add a flavour text to be send to the LLM for a _viewer-customized response
namespace BanterBrain_Buddy
{
    [SupportedOSPlatform("windows10.0.10240")]
    public partial class TwitchNotableViewers : Form
    {

        private static readonly log4net.ILog _bBBlog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private TwitchNotableViewerClass _viewer;
        public List<TwitchNotableViewerClass> viewers = [];
        private bool FlavourTextEdited = false;
        private bool AddingNewViewer = false;
        private int CurrSelectedViewerIndex;

        public TwitchNotableViewers()
        {
            InitializeComponent();
            TwitchNotableViewers_LoadFromFile();

            //is the form visible?
            DisplayViewers();


        }


        public List<TwitchNotableViewerClass> GetViewers()
        {
            TwitchNotableViewers_LoadFromFile();
            return viewers;
        }

        //aight so first we load any previously saved MyTwitchNotableViewers from the json file in format "Viewer name" : "Flavour text"
        //location of the file containing the saved MyTwitchNotableViewers is %AppData%\BanterBrain Buddy\MyTwitchNotableViewers.json
        //we make this public because we need to access it from the main form at the start of the program anyway
        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchNotableViewers_LoadFromFile()
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BanterBrain\\viewers.json";
            //aight now load the MyTwitchNotableViewers from the file into TwitchNotableViewerClass

            if (System.IO.File.Exists(path))
            {
                _bBBlog.Info("Twitch notable viewers file found, loading viewers from json file");
                string json = System.IO.File.ReadAllText(path);
                viewers = JsonConvert.DeserializeObject<List<TwitchNotableViewerClass>>(json) ?? new List<TwitchNotableViewerClass>();
                _bBBlog.Debug("Viewers loaded from file: " + viewers.Count);
            }
            else
            {
                //we create the file if it doesn't exist
                _bBBlog.Info("Twitch notable viewers file not found, creating new file");
                System.IO.File.Create(path).Dispose();
            }

            //we just loaded it, so nothing is changed!
            FlavourTextEdited = false;

            //if theres nothing, we dont need to enable the delete button
            if (viewers.Count < 1)
                ViewerDeleteButton.Enabled = false;
        }

        [SupportedOSPlatform("windows10.0.10240")]
        //So now we have the MyTwitchNotableViewers loaded into the MyTwitchNotableViewers list, we can display them in the listbox
        public void DisplayViewers()
        {
            if (viewers.Count > 0)
            {
                _bBBlog.Info("Adding viewers in the listbox");
                TwitchNotableViewersComboBox.Items.Clear();
                foreach (TwitchNotableViewerClass viewer in viewers)
                {
                    TwitchNotableViewersComboBox.Items.Add(viewer.ViewerName);
                }
                //set the selected index to the first viewer, but we need to disable the eventhandlers
                TwitchFlavourTextBox.TextChanged -= TwitchFlavourTextBox_TextChanged;
                TwitchNotableViewersComboBox.SelectedIndex = 0;
                TwitchFlavourTextBox.TextChanged += TwitchFlavourTextBox_TextChanged;
            }
            else
            {
                _bBBlog.Info("No viewers found, so why not add some!");
            }

        }

        [SupportedOSPlatform("windows10.0.10240")]
        //here we load the flavour text of the selected _viewer into the textbox so it can be edited or deleted when the combobox selection changes
        private void TwitchNotableViewersComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (CurrSelectedViewerIndex == TwitchNotableViewersComboBox.SelectedIndex && TwitchFlavourTextBox.Text.Length > 1)
            {
                _bBBlog.Info("Selected viewer not changed, no need to load flavour text");
                return;
            }

            _bBBlog.Info("Selected viewer changed, loading flavour text");
            TwitchFlavourTextBox.TextChanged -= TwitchFlavourTextBox_TextChanged;
            TwitchFlavourTextBox.Text = "";

            //the combo can fire this with no selection (-1) or an empty list, indexing would throw
            if (TwitchNotableViewersComboBox.SelectedIndex < 0 || TwitchNotableViewersComboBox.SelectedIndex >= viewers.Count)
            {
                _bBBlog.Debug("No valid viewer selected, nothing to load");
                TwitchFlavourTextBox.TextChanged += TwitchFlavourTextBox_TextChanged;
                return;
            }

            //if there is a flavour text, load it into the textbox
            if (viewers[TwitchNotableViewersComboBox.SelectedIndex].FlavourText == null)
            {
                _bBBlog.Debug("No flavour text found, this should not happen but sure whatever!");
                return;
            } else if (viewers[TwitchNotableViewersComboBox.SelectedIndex].FlavourText.Length > 1)
            {
                TwitchFlavourTextBox.Text = viewers[TwitchNotableViewersComboBox.SelectedIndex].FlavourText;
                FlavourTextEdited = false;
            }

            CurrSelectedViewerIndex = TwitchNotableViewersComboBox.SelectedIndex;
            TwitchFlavourTextBox.TextChanged += TwitchFlavourTextBox_TextChanged;
            //since we have a change in selected viewer, if any of the buttons are disabled, we enable them
            ViewerAddButton.Enabled = true;
            ViewerDeleteButton.Enabled = true;
        }

        //here we save the current class to the json file
        [SupportedOSPlatform("windows10.0.10240")]
        public void TwitchNotableViewers_SaveToFile()
        {

            string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BanterBrain\\viewers.json";
            _bBBlog.Info("Saving viewers to file");
            // Serialize the list of MyTwitchNotableViewers to JSON format
            string json = JsonConvert.SerializeObject(viewers, Formatting.Indented);

            // Write the JSON string to the file
            System.IO.File.WriteAllText(path, json);

        }


        private void ViewerAddButton_Click(object sender, EventArgs e)
        {
            //if the user is adding a new viewe, this button becomes the save button
            if (AddingNewViewer)
            {
                _bBBlog.Debug("Save button clicked, we should do the checks");
                //first we check if the username already exists, if so we give an error message
                //if the user does not exist, we save it to the file
                if (TwitchNotableViewersComboBox.Text.Length < 1 || TwitchFlavourTextBox.Text.Length < 1)
                {
                    _bBBlog.Info("That cant be empty buddy! Try again!");
                    MessageBox.Show("Viewer name or flavour text cannot be empty", "Error", MessageBoxButtons.OK);
                    //leets reload the original flavour text if there was any previously saved
                    return;
                }
                else if (viewers.Any(x => x.ViewerName == TwitchNotableViewersComboBox.Text))
                {
                    //viewer already exists, so we give an error that that cant be done
                    _bBBlog.Info("Viewer already exists, not saving anything!");
                    MessageBox.Show("Viewer already exists, please cancel or change the name", "Error", MessageBoxButtons.OK);
                    return;
                }
                else
                {
                    _bBBlog.Info("Adding new viewer to the collection");
                    //disable the event for selected index changed
                    TwitchNotableViewersComboBox.SelectedIndexChanged -= TwitchNotableViewersComboBox_SelectedIndexChanged;
                    TwitchFlavourTextBox.TextChanged -= TwitchFlavourTextBox_TextChanged;
                    _viewer = new TwitchNotableViewerClass();
                    _viewer.ViewerName = TwitchNotableViewersComboBox.Text;
                    _viewer.FlavourText = TwitchFlavourTextBox.Text;
                    viewers.Add(_viewer);
                    TwitchNotableViewersComboBox.Items.Add(_viewer.ViewerName);
                    //set the selected index to the new viewer
                    TwitchNotableViewersComboBox.SelectedIndex = TwitchNotableViewersComboBox.Items.Count - 1;
                    TwitchFlavourTextBox.Focus();
                    TwitchNotableViewersComboBox.SelectedIndexChanged += TwitchNotableViewersComboBox_SelectedIndexChanged;
                    TwitchFlavourTextBox.TextChanged += TwitchFlavourTextBox_TextChanged;
                    //aaand we save it to the file
                    TwitchNotableViewers_SaveToFile();
                    FlavourTextEdited = false;
                    ViewerAddButton.Enabled = true;
                    ViewerDeleteButton.Enabled = true;
                    //change it back to list so you cant just type there
                    TwitchNotableViewersComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                    //we need to reset the button names
                    AddingNewViewer = false;
                    ViewerAddButton.Text = "New";
                    ViewerDeleteButton.Text = "Delete";
                    CurrSelectedViewerIndex = TwitchNotableViewersComboBox.SelectedIndex;

                }
                return;
            }
            
            _bBBlog.Debug("Add viewer button clicked");
            //lets empty all the text box and combobox
            TwitchFlavourTextBox.Text = "";
            TwitchNotableViewersComboBox.DropDownStyle = ComboBoxStyle.Simple;
            TwitchNotableViewersComboBox.Focus();
            TwitchNotableViewersComboBox.Text = "";
            
            AddingNewViewer = true;
            //when adding a new viewer, the New button becomes the Save button and the Delete button becomes the cancel button
            //if the user decides to cancel, we load index 0 if available
            ViewerAddButton.Text = "Save";
            ViewerDeleteButton.Text = "Cancel";
            //ok we have to enable this cos its a different button for now
            ViewerDeleteButton.Enabled = true;
        }


        private void TwitchFlavourTextBox_TextChanged(object sender, EventArgs e)
        {
            if (TwitchNotableViewersComboBox.Text.Length > 1 && FlavourTextEdited == false)
            {
                _bBBlog.Debug("Flavourtext text changed handler");
                FlavourTextEdited = true;
            }
        }

        private void ViewerDeleteButton_Click(object sender, EventArgs e)
        {
            if (AddingNewViewer)
            {
                _bBBlog.Info("Cancel button clicked, so we cancel adding a new viewer!");
                //if the user was adding a new viewer and decided to cancel, we just load the first viewer
                AddingNewViewer = false;
                ViewerAddButton.Text = "New";
                ViewerDeleteButton.Text = "Delete";
                TwitchNotableViewersComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                //set the selected index to the first viewer if there is any
                if (TwitchNotableViewersComboBox.Items.Count == 0)
                {
                    _bBBlog.Debug("There is no viewer left, not setting index");
                    TwitchFlavourTextBox.Text = "";
                }
                else
                {
                    TwitchFlavourTextBox.Text = "";
                    TwitchNotableViewersComboBox.SelectedIndex = 0;
                    //and load the flavour text
                    TwitchFlavourTextBox.Text = viewers[TwitchNotableViewersComboBox.SelectedIndex].FlavourText;
                }
                return;
            }

            //here we delete the viewer from the list and the combobox, then save it to the file, byebye viewer!
            _bBBlog.Info("Delete viewer button clicked");
            if (viewers.Count < 1)
            {
                _bBBlog.Info("No viewers to delete");
                ViewerDeleteButton.Enabled = false;
                return;
            }

            //now first lets ask if they are sure
            DialogResult dialogResult = MessageBox.Show($"Are you sure you want to delete viewer {TwitchNotableViewersComboBox.Text}?", "Delete viewer", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No)
            {
                _bBBlog.Info("User decided not to delete the viewer");
                return;
            }
            //disable the event for selected index changed
            TwitchNotableViewersComboBox.SelectedIndexChanged -= TwitchNotableViewersComboBox_SelectedIndexChanged;
            TwitchFlavourTextBox.TextChanged -= TwitchFlavourTextBox_TextChanged;
            //remove the viewer from the list
            viewers.RemoveAt(TwitchNotableViewersComboBox.SelectedIndex);
            TwitchFlavourTextBox.Text = "";
            //remove the viewer from the combobox
            TwitchNotableViewersComboBox.Items.RemoveAt(TwitchNotableViewersComboBox.SelectedIndex);

            //enable the event for selected index changed
            TwitchNotableViewersComboBox.SelectedIndexChanged += TwitchNotableViewersComboBox_SelectedIndexChanged;
            TwitchFlavourTextBox.TextChanged += TwitchFlavourTextBox_TextChanged;
            //set the selected index to the first viewer
            if (TwitchNotableViewersComboBox.Items.Count == 0)
            {
                _bBBlog.Debug("There is no viewer left, not setting index");
                TwitchFlavourTextBox.Text = "";
            }
            else
                TwitchNotableViewersComboBox.SelectedIndex = 0;
            //save the changes to the file
            TwitchNotableViewers_SaveToFile();
            if (viewers.Count < 1)
                ViewerDeleteButton.Enabled = false;
            else
                ViewerDeleteButton.Enabled = true;
        }

        private void TwitchNotableViewersComboBox_Validating(object sender, CancelEventArgs e)
        {
            _bBBlog.Debug("Validating");
            //if flavour text is empty, we dont allow the user to leave the combobox
            if (TwitchFlavourTextBox.Text.Length < 1)
            {
                _bBBlog.Info("Flavour text is empty, not allowing to leave the combobox");
                //show message
                MessageBox.Show("Flavour text cannot be empty", "Error", MessageBoxButtons.OK);
                //lets load the previous flavour text if there was any, both indexes have to be valid
                if (TwitchNotableViewersComboBox.SelectedIndex >= 0 && TwitchNotableViewersComboBox.SelectedIndex < viewers.Count
                    && CurrSelectedViewerIndex >= 0 && CurrSelectedViewerIndex < viewers.Count)
                {
                    TwitchFlavourTextBox.TextChanged -= TwitchFlavourTextBox_TextChanged;
                    TwitchFlavourTextBox.Text = viewers[CurrSelectedViewerIndex].FlavourText;
                    TwitchFlavourTextBox.TextChanged += TwitchFlavourTextBox_TextChanged;
                }
                e.Cancel = true;
            }
            else
            {
                _bBBlog.Info("Flavour text is not empty, allowing to leave the combobox");
                e.Cancel = false;
            }

        }

        private void TwitchFlavourTextBox_Leave(object sender, EventArgs e)
        {
            _bBBlog.Debug("Flavour text box leave");
            //if its an existing viewer and the flavour text was edited, save it
            //flavour text cannot be empty by the way
            if (FlavourTextEdited && TwitchFlavourTextBox.Text.Length < 1)
            {
                _bBBlog.Info("Flavour text is empty, not saving anything!");
                return;
            }

            if (viewers.Any(x => x.ViewerName == TwitchNotableViewersComboBox.Text) & FlavourTextEdited)
            {
                _bBBlog.Info("Viewer already exists, and flavour text changed. Saving to file");
                _bBBlog.Debug("Viewer name: " + TwitchNotableViewersComboBox.Text);
                //ok so aparently the focustext changed, so lets update the viewer and save it to the file
                _viewer = viewers.Find(x => x.ViewerName == TwitchNotableViewersComboBox.Text);
                _viewer.FlavourText = TwitchFlavourTextBox.Text;
                TwitchNotableViewers_SaveToFile();
                FlavourTextEdited = false;
                ViewerAddButton.Enabled = true;
                ViewerDeleteButton.Enabled = true;

            }
        }
    }
}
