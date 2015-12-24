﻿namespace uAssist.EditorDesigner
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Collections.Generic;
    using uAssist.UEditorWidgets;
    using UnityEngine;
    using System.Collections;
    using uAssist.Forms;
    

    public class UFormCodeGen
    {

#region Code Templates

        private const string _codeTemplateBack = @"
//This code generated by tool. Do not hand update!
//ALSO DO NOT DELETE OR YOU WILL LOOSE YOUR FORM!
namespace %%NAMESPACE%%
{
    using UnityEngine;
    using UnityEditor;
    using System.Collections;
    using System.Collections.Generic;
    using uAssist.UEditorWidgets;
    using uAssist.UEditorWidgets.Internal;
    using uAssist.Forms;

    [UWidgetForm]
    public partial class %%FORM_NAME%% : frmBase
    {

%%FORM_MENU%%

        //Widget declarations
%%WIDGET_DECS%%

        public override void InitalizeComponents()
        {

        //Form settings
%%FORM_SETTINGS%%
            
        //Property settings
%%WIDGET_PROPS%%

        }

    }
}";

        private const string _codeTemplateFront = @"
namespace %%NAMESPACE%%
{
    using UnityEngine;
    using UnityEditor;
    using System.Collections;
    using System.Collections.Generic;
    using uAssist.UEditorWidgets;
    using uAssist.Forms;

    public partial class %%FORM_NAME%% : frmBase
    {

#region Local vars and helpers

#endregion


#region UEditor plumbing

        //Public form constructor
        public %%FORM_NAME%%() : base()
        {

            //Subscribe to all required events
            this.EnableEvents();

            //Bind widget controls
            this.SetupBindings();

        }

        protected override void EnableEvents() 
        {
            base.EnableEvents();

            //Wire in widget events here. For Example:
            //_btnAddLabel.OnClick += this._btnAddLabel_OnClick;

        }

        protected override void DisableEvents()
        {
            base.DisableEvents();

            //Disable events here. For example:
            //_btnAddLabel.OnClick -= this._btnAddLabel_OnClick;

            //NOTE: This method is called by the UEditor Designer when loading a form in for editing.
            //Any activity required to make the form safe for editing including unsubscribing events and bindings should go here.

        }

        private void SetupBindings()
        {
            //Setup widget bindings. For example.
            //_btnAddLabel.BindTo(this, this.Name);

        }

#endregion


#region Widget event handlers

#endregion


#region Functional code

#endregion

    }
}";

#endregion
        
        private frmBase _form;

        public List<UEditorWidgetBase> Widgets
        {
            get
            {
                return this._form.Children;
            }
            set
            {
                this._form.Children = value;
            }

        }

        public string GeneratedClassName = "";

        private string _codeBehind;
        public string CodeBehind
        {
            get
            {
                return _codeBehind;
            }
        }

        private string _codeFront;
        public string CodeFront
        {
            get
            {
                return _codeFront;
            }
        }

        private Hashtable _widgetObjectKeys = new Hashtable();
        private List<string> _widgetPropertySetters = new List<string>();
        private List<string> _widgetDeclarations = new List<string>();
        private List<UEditorWidgetBase> _rootContainer = new List<UEditorWidgetBase>();

        //Public constuctor
        public UFormCodeGen (frmBase Form)
        {
            _form = Form;
        }

        public Dictionary<string, string> FormSettings = new Dictionary<string, string>();

        public void PharseForm()
        {
            if (this.GeneratedClassName == "")
            {
                throw new Exception("Class name not specified in UformCodeGen.PharseForm()");
            }

            _rootContainer = this.Widgets.Cast<UEditorWidgetBase>().ToList();

            _widgetObjectKeys.Clear();
            ValidateNames_Recrusive(_rootContainer);

            _widgetDeclarations.Clear();
            _widgetPropertySetters.Clear();
            ProcessWidgets_Recrusive(_rootContainer);



            //Build the code behind 
            //=====================
            _codeBehind = _codeTemplateBack;

            //Set the form class name
            _codeBehind = _codeBehind.Replace("%%FORM_NAME%%", this.GeneratedClassName);

            //Update the Namespace
            if  (this._form.DesignerNameSpace == "")
            {
                _codeBehind = _codeBehind.Replace("%%NAMESPACE%%", "uAssist.Forms");
            }
            else
            {
                _codeBehind = _codeBehind.Replace("%%NAMESPACE%%", this._form.DesignerNameSpace);
            }

            //Update the form settings
            string __formSettings = "\t\tthis.name = \"" + this.GeneratedClassName + "\";\r\n";
            foreach (var item in this.FormSettings)
            {
                __formSettings += "\t\tthis." + item.Key + " = " + item.Value + ";\r\n";
            }
            _codeBehind = _codeBehind.Replace("%%FORM_SETTINGS%%", __formSettings);

            //Create the code menu or remove the tag
            if (this._form.AutoMenuOption == true)
            {
                string __menuOption = "[MenuItem(\"Window/Forms/" + this._form.FormTitle + "\")]\r\n";
                __menuOption += "\t\tpublic static void OpenWindow()\r\n";
                __menuOption += "\t\t{\r\n";
                __menuOption += "\t\t\t" + "UnityEditor.EditorWindow.GetWindow<" + this.GeneratedClassName + ">();\r\n";
                __menuOption += "\t\t}";
                _codeBehind = _codeBehind.Replace("%%FORM_MENU%%", __menuOption);
            }
            else
            {
                _codeBehind = _codeBehind.Replace("%%FORM_MENU%%", "");
            }

            //Property declarations
            string __propDecs = "";
            foreach (var item in _widgetDeclarations)
            {
                __propDecs += "\t\t" + item;
                __propDecs += "\r\n";
            }
            _codeBehind = _codeBehind.Replace("%%WIDGET_DECS%%", __propDecs);

            //Property setters
            string __propSetters = "";
            foreach (var item in _widgetPropertySetters)
            {
                __propSetters += "\t" + item;
                __propSetters += "\r\n";
            }
            _codeBehind = _codeBehind.Replace("%%WIDGET_PROPS%%", __propSetters);
            


            //Build the code front
            //====================
            _codeFront = _codeTemplateFront;
            _codeFront = _codeFront.Replace("%%FORM_NAME%%", this._form.name);

            //Update the Namespace
            if (this._form.DesignerNameSpace == "")
            {
                _codeFront = _codeFront.Replace("%%NAMESPACE%%", "uAssist.Forms");
            }
            else
            {
                _codeFront = _codeFront.Replace("%%NAMESPACE%%", this._form.DesignerNameSpace);
            }
            
        }

        private void ValidateNames_Recrusive(List<UEditorWidgetBase> CurrentContainer)
        {
            foreach (var item in CurrentContainer)
            {
                if (_widgetObjectKeys.ContainsKey(item.Name) == true)
                {
                    int __nameIndex = 1;
                    while (_widgetObjectKeys.ContainsKey(item.Name +__nameIndex.ToString()) == true)
                    {
                        __nameIndex++;
                    }
                    item.Name += __nameIndex.ToString();
                }
                _widgetObjectKeys.Add(item.Name, item);

                if (item.GetType().IsSubclassOf(typeof (UEditorPanelBase)))
                {
                    UEditorPanelBase __castPanel = (UEditorPanelBase)item;
                    ValidateNames_Recrusive(__castPanel.Children); //And down the rabbit hole we go
                }
            }
        }

        private void ProcessWidgets_Recrusive(List<UEditorWidgetBase> CurrentContainer)
        {
            List<UEditorWidgetBase> __nestedWidgets = new List<UEditorWidgetBase>();

            foreach (var item in CurrentContainer)
            {
                UEditorWidgetBase __castWidget = (UEditorWidgetBase)item;
                UWidgetCodeGen __cGen = new UWidgetCodeGen(__castWidget);

                _widgetDeclarations.Add(__cGen.WidgetDeclaration);

                _widgetPropertySetters.Add("\t//Property setters for ->" + item.Name);

                foreach (var setter in __cGen.PropertySetters)
                {
                    _widgetPropertySetters.Add("\t" + setter);
                }
                _widgetPropertySetters.Add("\t" + __cGen.WidgetParent);
                _widgetPropertySetters.Add("");
                _widgetPropertySetters.Add("");

                if (item.GetType().IsSubclassOf(typeof (UEditorPanelBase)))
                {
                    UEditorPanelBase __castPanel = (UEditorPanelBase)item;
                    __nestedWidgets.AddRange(__castPanel.Children.Cast<UEditorWidgetBase>().ToList());
                }
            }

            if (__nestedWidgets.Count > 0)
            {
                ProcessWidgets_Recrusive(__nestedWidgets);
            }
        }
    }
}