#author = 'Billy Basinger'

# Load the Python Standard and DesignScript Libraries
import sys
import clr
clr.AddReference('ProtoGeometry')
from Autodesk.DesignScript.Geometry import *

clr.AddReference('RevitAPI')
clr.AddReference('RevitAPIUI')
import Autodesk
from Autodesk.Revit.DB import *
from Autodesk.Revit.UI.Selection import *
from Autodesk.Revit.UI import TaskDialog

clr.AddReference('RevitServices')
import RevitServices

from RevitServices.Persistence import DocumentManager

clr.AddReference('RevitServices')
from RevitServices.Persistence import DocumentManager
from RevitServices.Transactions import TransactionManager

from System.Collections.Generic import List

import clr
clr.AddReference('System.Windows.Forms')
from System.Windows.Forms import MessageBox

import clr
clr.AddReference('System.Windows.Forms')
clr.AddReference('System.Drawing')
from System.Windows.Forms import Application, Form, RadioButton, Button, DialogResult, FormStartPosition, FormBorderStyle, CheckBox, Panel, FlowLayoutPanel, ScrollBars, DockStyle, Padding, FlowDirection
from System.Drawing import Point, Size

from System import Array

# The inputs to this node will be stored as a list in the IN variables.

#####################################################

doc = DocumentManager.Instance.CurrentDBDocument
uidoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument

#####################################################

run = (IN[0])
    
#####################################################

#Function for revit link user selection.
def show_revit_link_dialog():
    form = Form()
    form.Text = "Select Linked Revit Model to Process"
    form.ClientSize = Size(400, 500) 
    form.StartPosition = FormStartPosition.CenterScreen
    #Create a panel that contains the radio checkBox.
    panel = FlowLayoutPanel()
    #Dock the panel to the top of the window.
    panel.Dock = DockStyle.Top 
    panel.Height = 380
    #Enable scrolling if the amount of links exceeds the panel's height.
    panel.AutoScroll = True
    #Arrange the controls from the top down.
    panel.FlowDirection = FlowDirection.TopDown
    #Do not wrap text.
    panel.WrapContents = False
    #Add some padding around the panel.
    panel.Padding = Padding(5)
    #Collect revit link models.
    links = FilteredElementCollector(doc).OfClass(RevitLinkInstance).ToElements()
    checkBoxRevitLinks = []
    #Function to enforce single checkbox selection.
    def force_single_selection(selected_checkBox):
        #Iterate checkbox links.
        for checkBoxes in checkBoxRevitLinks:
            #Check if the checkbox is not the one that is clicked.
            if checkBoxes != selected_checkBox:
                #If true, uncheck it.
                checkBoxes.Checked = False
    #Create a checkbox for every revit link.
    for link in links:
        checkBoxRevitLink = CheckBox()
        #Split unnecessary strings from revit model names and return index 0 which is the string recognized by the user. 
        checkBoxRevitLink.Text = link.Name.split(':')[0]
        #Store the revit link in the tag property. Tag = an object that contains data about the control. 
        #Control in this case = revit link radio button. 
        checkBoxRevitLink.Tag = link
        #Auto size the radio button to fit the text.
        checkBoxRevitLink.AutoSize = True
        #Add some margin.
        checkBoxRevitLink.Margin = Padding(10)
        #Add an event to use the single selection function.
        def on_click(sender, event):
            #Execute the function.
            force_single_selection(sender)
        #Add the click event to the function.
        checkBoxRevitLink.Click += on_click
        #Add the radio button to the panel.
        panel.Controls.Add(checkBoxRevitLink)
        #Append the radio checkBox to the list.
        checkBoxRevitLinks.append(checkBoxRevitLink)
    okButton = Button()
    okButton.Text = "OK"
    okButton.Width = 80
    okButton.Height = 30
    okButton.Location = Point(125, 450)
    cancelButton = Button()
    cancelButton.Text = "Cancel"
    cancelButton.Width = 80
    cancelButton.Height = 30
    cancelButton.Location = Point(225, 450)
    def on_ok_button_clicked(sender, event):
        global selected_link
        #Iterate radio checkBox list.
        for checkBox in checkBoxRevitLinks:
            #Check which radio button representing the revit link is checked.
            if checkBox.Checked:
                #If checked, store the revit link to the form's tag property.
                form.Tag = checkBox.Tag
                #Split unnecessary strings from revit model names and return index 0 which is the string recognized by the user.
                selected_link = form.Tag.Name.split(':')[0]
                break
        form.DialogResult = DialogResult.OK
    okButton.Click += on_ok_button_clicked
    cancelButton.DialogResult = DialogResult.Cancel
    form.Controls.Add(panel)
    form.Controls.Add(okButton)
    form.Controls.Add(cancelButton)
    Application.EnableVisualStyles()
    return form.ShowDialog()

#####################################################

# Assign your output to the OUT variable.
if run == 1:
    #Proceed to the next dialog.   
    revit_link_result = show_revit_link_dialog()
    #Check if user selected 'ok'button.
    if revit_link_result == DialogResult.OK:
        #If both dialogs were checked 'ok' output the user selections.
        OUT = selected_link 
    else:
        #If the user cancelled the operation, the output will indicate so.
        OUT = "The user cancelled the operation."
else:
    #Scrip set not to run. 
    OUT = "The Run Node is set to 0."