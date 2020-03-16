/**********************************************************
 * Demo for Standalone SDK.Created by Darcy on Oct.15 2009*
***********************************************************/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using System.Threading;
using System.Data.SqlClient;
using System.IO;

namespace Card
{
    public partial class CardMain : Form
    {
        public CardMain()
        {
            InitializeComponent ();
            if (System.Windows.Forms.Screen.AllScreens.Length > 1)
            {
                this.Location = Screen.AllScreens[1].Bounds.Location;
                this.WindowState = FormWindowState.Maximized;
            }
           
        }

        //Create Standalone SDK class dynamicly.
        public zkemkeeper.CZKEMClass axCZKEM1 = new zkemkeeper.CZKEMClass ();
        #region Communication
        private bool bIsConnected = false;//the boolean value identifies whether the device is connected
        private int iMachineNumber = 1;//the serial number of the device.After connecting tehe device ,this value will be changed.
        dataBaseClass dbClass = new dataBaseClass ();
        #endregion

        #region RealTime Events
        private void rtTimer_Tick(object sender, EventArgs e)
        {
            if (axCZKEM1.ReadRTLog (iMachineNumber))
            {
                while (axCZKEM1.GetRTLog (iMachineNumber))
                {
                    ;
                }
            }
        }
        #endregion

        #region Card Operation
        bool bAddControl = true;
        private void UserIDTimer_Tick(object sender, EventArgs e)
        {
            if (bIsConnected == false)
            {

                bAddControl = true;
                return;
            }
            else
            {
                if (bAddControl == true)
                {
                    string sEnrollNumber = "";
                    string sName = "";
                    string sPassword = "";
                    int iPrivilege = 0;
                    bool bEnabled = false;

                    axCZKEM1.EnableDevice (iMachineNumber, false);
                    axCZKEM1.ReadAllUserID (iMachineNumber);//read all the user information to the memory
                    while (axCZKEM1.SSR_GetAllUserInfo (iMachineNumber, out sEnrollNumber, out sName, out sPassword, out iPrivilege, out bEnabled))
                    {
                        //cbUserID.Items.Add(sEnrollNumber);
                    }
                    bAddControl = false;
                    axCZKEM1.EnableDevice (iMachineNumber, true);
                }
                return;
            }
        }


        #endregion

        private void timer_Close_Tick(object sender, EventArgs e)
        {
            this.Close ();
        }
    }
}