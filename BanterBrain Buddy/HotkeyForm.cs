using Gma.System.MouseKeyHook;
using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Windows.Forms;

/// <summary>
/// CODING RULES:
/// •	Local variables, private instance, static fields and method parameters should be camelCase.
/// •	Methods, constants, properties, events and classes should be PascalCase.
/// </summary>

namespace BanterBrain_Buddy
{
    public partial class HotkeyForm : Form
    {
        private static readonly log4net.ILog _bBBlog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
       // private bool _isKeyReleased;

        public HotkeyForm()
        {
            InitializeComponent();
        }

        private IKeyboardMouseEvents m_GlobalHook;

        [SupportedOSPlatform("windows10.0.10240")]
        public void Subscribe()
        {
            m_GlobalHook = Hook.GlobalEvents();
            m_GlobalHook.KeyDown += GlobalHookKeyDown;
            m_GlobalHook.KeyUp += GlobalHookKeyUp;
        }

        readonly List<Keys> KeyCombo = [];

        public List<Keys> ReturnValue1 { get; set; }

        [SupportedOSPlatform("windows10.0.10240")]
        private void GlobalHookKeyDown(object sender, KeyEventArgs e)
        {
            //TODO: ALT is a bad idea, so fix that sometime
            //logic: get any keyvalue on key down and show it in the textbox
            //once any key up, then return the keycombo
            //if escape, then cancel
            if (!KeyCombo.Contains(e.KeyCode))
            {
                KeyCombo.Add(e.KeyCode);
                _bBBlog.Info($"KeyCombo: {KeyCombo}");
                // CombiText.Text += (char)e.KeyValue;
                CombiText.Text += e.KeyCode + " ";
            }
            //Escape
            if (e.KeyValue == 27)
            {
                this.ReturnValue1 = null;
                Unsubscribe();
                this.Close();
            }

        }

        [SupportedOSPlatform("windows10.0.10240")]
        public void Unsubscribe()
        {
            //the escape key path calls this and Close() triggers FormClosing which calls it again,
            //so guard against a null hook (form closed before Shown) and a double dispose
            if (m_GlobalHook == null)
            {
                return;
            }
            try
            {
                m_GlobalHook.KeyDown -= GlobalHookKeyDown;
                m_GlobalHook.KeyUp -= GlobalHookKeyUp;
                //It is recommened to dispose it
                m_GlobalHook.Dispose();
            }
            catch (ObjectDisposedException)
            {
                _bBBlog.Debug("Hotkey hook already disposed");
            }
            finally
            {
                m_GlobalHook = null;
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        public void GlobalHookKeyUp(object sender, KeyEventArgs e)
        {
            _bBBlog.Info("Key Up event");
           // _isKeyReleased = true;

            //CombiText.Text += (char)e.KeyValue;
            this.ReturnValue1 = KeyCombo;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }


        [SupportedOSPlatform("windows10.0.10240")]
        private void HotkeyForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Just incase something left over.
            Unsubscribe();
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void HotkeyForm_Shown(object sender, EventArgs e)
        {
            DontPingTextBox.Focus();
            Subscribe();
        }

    }
}
