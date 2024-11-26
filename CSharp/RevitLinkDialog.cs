using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IfcRoomToRvtRoom
{
    // Define a form to select linked Revit model.s
    public partial class RevitLinkDialog : System.Windows.Forms.Form
    {
        // Panel to hold check boxes.
        private FlowLayoutPanel panel;

        // Ok Button.
        private Button okButton;

        // Cancel Button.
        private Button cancelButton;

        // List to hold the check boxes for each Revit Link in the model.
        private List<CheckBox> checkBoxRevitLinks = new List<CheckBox>();

        // Property to store the selected Revit link.
        public RevitLinkInstance SelectedRevitLink { get; set; }

        // Constructor to initialize the form.
        public RevitLinkDialog(Document doc)
        {
            // Set the properties of the form.
            this.Text = "Select Linked Revit Model to Process";
            this.Size = new System.Drawing.Size(450, 500);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Create the FlowLayoutPanel to contain the check boxes.
            panel = new FlowLayoutPanel();
            panel.Dock = DockStyle.Top;
            panel.Height = 380;
            panel.AutoScroll = true;
            panel.FlowDirection = FlowDirection.TopDown;
            panel.WrapContents = false;
            panel.Padding = new Padding(5);

            // Collect all the linked Revit models in the project.
            FilteredElementCollector links = new FilteredElementCollector(doc).OfClass(typeof(RevitLinkInstance));

            // Iterate each Revit link.
            foreach (RevitLinkInstance link in links)
            {
                CheckBox checkBoxRevitLink = new CheckBox();
                // Create a string array to split unnecessary characters from the Revit model names.
                string[] splitName = link.Name.Split(':');

                // Check if the length of the split names returns more than 0 items.
                if (splitName.Length > 0)
                {
                    // If true, collect the item at index 0 which will be the Revit model without unwanted characters.
                    checkBoxRevitLink.Text = splitName[0];
                }
                // Attach the link object to the button's tag.
                checkBoxRevitLink.Tag = link;
                checkBoxRevitLink.Width = 100;
                checkBoxRevitLink.AutoSize = true;
                checkBoxRevitLink.Margin = new Padding(10);

                // Attach a checkbox changed event handler to enforce single selection.
                checkBoxRevitLink.CheckedChanged += (seder, args) =>
                {
                    if (checkBoxRevitLink.Checked)
                    {
                        // Iterate and uncheck all ofther checkboxes.
                        foreach (CheckBox allOtherCheckBoxes in checkBoxRevitLinks)
                        {
                            // Check if all other checkboxes do not equal the selected link.
                            if (allOtherCheckBoxes != checkBoxRevitLink)
                            {
                                // If true, set them to false.
                                allOtherCheckBoxes.Checked = false;
                            }
                        }
                    }
                };

                checkBoxRevitLinks.Add(checkBoxRevitLink);
                
                // Add the check box to the panel.
                panel.Controls.Add(checkBoxRevitLink);
            }
            // Create the OK button.
            okButton = new Button();
            okButton.Text = "OK";
            okButton.Width = 80;
            okButton.Height = 30;
            okButton.Location = new System.Drawing.Point(125, 400);

            // Attach the click event handler.
            okButton.Click += OkButton_Click;

            // Create cancel button.
            cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.Width = 100;
            cancelButton.Height = 30;
            cancelButton.Location = new System.Drawing.Point(225, 400);

            // Attach the click event handler.
            cancelButton.Click += cancelButton_Click;

            // Add controls to the form
            this.Controls.Add(panel);
            this.Controls.Add(okButton);
            this.Controls.Add(cancelButton);
        }
        // Event handler for OK button click event..
        private void OkButton_Click(object sender, EventArgs e)
        {
            // Iterate through the check boxes and find the one that is selected.
            foreach (CheckBox checkBox in checkBoxRevitLinks)
            {
                // Check if one of the check boxes (revit links) has been selected.
                if (checkBox.Checked)
                {
                    // If true, store the revit link to the form's tag property.
                    SelectedRevitLink = checkBox.Tag as RevitLinkInstance;
                    break;
                }
            }
            // Check if the user clicked the OK button without making a revit link selection.
            if (SelectedRevitLink == null)
            {
                // If true, output the message below. 
                MessageBox.Show("No link was selected. Please select a Revit link.");
            }
            else
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
        // Event handler for Cancel button click event.
        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
        // Static method to show the Revit link dialog and return the selected link.
        public static RevitLinkInstance ShowRevitLinkDialog(Document doc)
        {
            // Create and show the dialog.
            RevitLinkDialog dialog = new RevitLinkDialog(doc);

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                // Return the selected link.
                return dialog.SelectedRevitLink;
            }
            // Return null if no link was selected.
            return null;
        }
        // Event handler for form load event.
        private void RevitLinkDialog_Load(object sender, EventArgs e)
        {

        }
    }
}
