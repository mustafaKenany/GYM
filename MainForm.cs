using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
namespace Card
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent ();
        }
        public zkemkeeper.CZKEMClass axCZKEM1 = new zkemkeeper.CZKEMClass ();
        private bool bIsConnected = false;//the boolean value identifies whether the device is connected
        private int iMachineNumber = 1;//the serial number of the device.After connecting the device ,this value will be changed.
        dataBaseClass dbClass = new dataBaseClass ();
        string temproaryFlag = "";

        #region functions

        private bool dvDeleteUsr()
        {
            if (bIsConnected == false)
            {
                MessageBox.Show ("Please connect the device first!", "Error");
                return false;
            }

            if (cbUserIDDE.Text.Trim () == "" || cbBackupDE.Text.Trim () == "")
            {
                MessageBox.Show ("Please input the UserID and BackupNumber first!", "Error");
                return false;
            }
            int idwErrorCode = 0;

            string sUserID = cbUserIDDE.Text.Trim ();
            int iBackupNumber = Convert.ToInt32 (cbBackupDE.Text.Trim ());

            Cursor = Cursors.WaitCursor;
            if (axCZKEM1.SSR_DeleteEnrollData (iMachineNumber, sUserID, iBackupNumber))
            {
                axCZKEM1.RefreshData (iMachineNumber);//the data in the device should be refreshed
                lbRTShow.Items.Add ("تمت عملية المسح بنجاح");
                return true;
            }
            else
            {
                axCZKEM1.GetLastError (ref idwErrorCode);
                MessageBox.Show ("Operation failed,ErrorCode=" + idwErrorCode.ToString (), "Error");
                return false;
            }
        }

        private bool dvSaveNewUsr()
        {
            if (bIsConnected == false)
            {
                MessageBox.Show ("Please connect the device first!", "Error");
                return false;
            }

            if (txtUserID.Text.Trim () == "" || cbPrivilege.Text.Trim () == "" || txtCardnumber.Text.Trim () == "")
            {
                MessageBox.Show ("UserID,Privilege,Cardnumber must be inputted first!", "Error");
                return false;
            }
            if (dtpStartDate.Value.Date < DateTime.Now.Date)
            {
                dtpStartDate.Focus ();
                MessageBox.Show ("يجب ادخال تاريخ صحيح", "Message");
                return false;
            }
            if (dtpEndDate.Value.Date < DateTime.Now.Date)
            {
                MessageBox.Show ("يجب ادخال تاريخ صحيح", "Message");
                dtpEndDate.Focus ();
                return false;
            }
            int idwErrorCode = 0;

            bool bEnabled = true;
            if (chbEnabled.Checked)
            {
                bEnabled = true;
            }
            else
            {
                bEnabled = false;
            }
            string sdwEnrollNumber = txtUserID.Text.Trim ();
            string sName = txtName.Text.Trim ();
            string sPassword = txtPassword.Text.Trim ();
            int iPrivilege = Convert.ToInt32 (cbPrivilege.Text.Trim ());
            string sCardnumber = txtCardnumber.Text.Trim ();

            Cursor = Cursors.WaitCursor;
            axCZKEM1.EnableDevice (iMachineNumber, false);
            axCZKEM1.SetStrCardNumber (sCardnumber);//Before you using function SetUserInfo,set the card number to make sure you can upload it to the device
            if (axCZKEM1.SSR_SetUserInfo (iMachineNumber, sdwEnrollNumber, sName, sPassword, iPrivilege, bEnabled))//upload the user's information(card number included)
            {
                lbRTShow.Items.Clear ();
                lbRTShow.Items.Add ("تمت عملية الاضافة بنجاح");
                axCZKEM1.RefreshData (iMachineNumber);//the data in the device should be refreshed
                axCZKEM1.EnableDevice (iMachineNumber, true);
                return true;
            }
            else
            {
                axCZKEM1.GetLastError (ref idwErrorCode);
                MessageBox.Show ("Operation failed,ErrorCode=" + idwErrorCode.ToString (), "Error");
                return false;
            }
            axCZKEM1.RefreshData (iMachineNumber);//the data in the device should be refreshed
            axCZKEM1.EnableDevice (iMachineNumber, true);

        }

        private void clear()
        {
            txtCardnumber.Text = "";
            txtName.Text = "";
            PBusr.Image = null;
            chIronAcc.Checked = false;
            chGymAcc.Checked = false;
            txtUserID.Text = dbClass.FetchUsrID ();
            dtpStartDate.Enabled = true;
            dtpEndDate.Enabled = true;

            txtUserID.Enabled = txtName.Enabled = txtCardnumber.Enabled = PBusr.Enabled = chIronAcc.Enabled = chGymAcc.Enabled = true;
        }

        private void dbSaveNewUsr()
        {
            SqlParameter[] param = new SqlParameter[7];
            param[0] = new SqlParameter ("@UsrName", SqlDbType.NVarChar, 50);
            param[0].Value = txtName.Text;
            param[1] = new SqlParameter ("@UsrCard", SqlDbType.BigInt);
            if (txtCardnumber.Text == "بصمة")
            {
                param[1].Value = 0;
            }
            else
            {
                param[1].Value = Convert.ToInt32 (txtCardnumber.Text);
            }

            param[2] = new SqlParameter ("@UsrPic", SqlDbType.Image);
            param[2].Value = ConvertImageToBinary (PBusr.Image);
            param[3] = new SqlParameter ("@UsrAcc", SqlDbType.NVarChar, 50);
            if (chIronAcc.Checked)
            {
                param[3].Value = "حديد";
            }
            if (chGymAcc.Checked)
            {
                param[3].Value = "رشاقة";
            }
            if (chIronAcc.Checked && chGymAcc.Checked)
            {
                param[3].Value = "مشترك";
            }
            param[4] = new SqlParameter ("@sDate", SqlDbType.Date);
            param[4].Value = dtpStartDate.Value;
            param[5] = new SqlParameter ("@eDate", SqlDbType.Date);
            param[5].Value = dtpEndDate.Value;
            param[6] = new SqlParameter ("@usrId", SqlDbType.BigInt);
            if (btnSetStrCardNumber.Text == "حفظ")
            {
                param[6].Value = int.Parse (dbClass.FetchUsrID ());
                dbClass.ExecuteCommand ("IncrementUsrID", null);
            }
            else
            {
                param[6].Value = int.Parse (txtUserID.Text);
            }
            dbClass.ExecuteCommand ("InsertNewUsr", param);

        }

        byte[] ConvertImageToBinary(Image img)
        {
            using (MemoryStream ms = new MemoryStream ())
            {
                img.Save (ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                return ms.ToArray ();
            }
        }

        private void SelectAllUSr(DataGridView view)
        {
            Cursor = Cursors.WaitCursor;
            var dt = new DataTable ();
            dt.Rows.Clear ();
            dt = dbClass.selectdata ("AllUsers", null);
            if (dt.Rows.Count > 0)
            {
                view.DataSource = dt;
            }
            else
            {
                MessageBox.Show ("لا توجد معلومات");
            }
            Cursor = Cursors.Default;
        }

        private void dvLog()
        {
            SqlParameter[] parm = new SqlParameter[4];
            parm[0] = new SqlParameter ("@ID", SqlDbType.BigInt);
            parm[1] = new SqlParameter ("@name", SqlDbType.NVarChar, 50);
            parm[2] = new SqlParameter ("@card", SqlDbType.BigInt);
            parm[3] = new SqlParameter ("@Acc", SqlDbType.NVarChar, 50);
            parm[0].Value = int.Parse (lbMonitorID.Text);
            parm[1].Value = lbMonitorName.Text;
            parm[2].Value = int.Parse (lbMonitorCard.Text);
            parm[3].Value = lbMonitorAcc.Text;
            dbClass.ExecuteCommand ("InsertDeviceLog", parm);
        }

        private void checkExpireUsrs()
        {
            DataTable dt = new DataTable ();
            SqlParameter[] param = new SqlParameter[1];
            param[0] = new SqlParameter ("@ID", SqlDbType.BigInt);
            dt = dbClass.selectdata ("SelectExpireUsr", null);
            if (dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    var usrId = dt.Rows[i]["UsrId"].ToString ();
                    param[0].Value = int.Parse (usrId);
                    cbUserIDDE.Items.Clear ();
                    cbUserIDDE.Items.Add (usrId);
                    cbUserIDDE.SelectedIndex = 0;
                    if (dvDeleteUsr () != false)
                    {
                        dbClass.ExecuteCommand ("updateDeletedID", param);
                        dbClass.deleteExistUsr (int.Parse (usrId));
                        Cursor = Cursors.Default;
                        cbUserIDDE.Items.Clear ();
                    }

                }

            }
        }

        private void SearchUsr(DataGridView X, string filter)
        {
            SqlParameter[] param = new SqlParameter[1];
            param[0] = new SqlParameter ("@Filter", SqlDbType.NVarChar, 50);
            param[0].Value = filter;
            DataTable dt = new DataTable ();
            dt = dbClass.selectdata ("FilterUsrs", param);
            X.DataSource = dt;
        }
        #endregion

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (txtIP.Text.Trim () == "" || txtPort.Text.Trim () == "")
            {
                MessageBox.Show ("IP and Port cannot be null", "Error");
                return;
            }
            int idwErrorCode = 0;

            Cursor = Cursors.WaitCursor;
            if (btnConnect.Text == "DisConnect")
            {
                axCZKEM1.Disconnect ();

                this.axCZKEM1.OnVerify -= new zkemkeeper._IZKEMEvents_OnVerifyEventHandler (axCZKEM1_OnVerify);
                //this.axCZKEM1.OnAttTransactionEx -= new zkemkeeper._IZKEMEvents_OnAttTransactionExEventHandler (axCZKEM1_OnAttTransactionEx);
                //this.axCZKEM1.OnNewUser -= new zkemkeeper._IZKEMEvents_OnNewUserEventHandler (axCZKEM1_OnNewUser);
                this.axCZKEM1.OnHIDNum -= new zkemkeeper._IZKEMEvents_OnHIDNumEventHandler (axCZKEM1_OnHIDNum);
                //this.axCZKEM1.OnWriteCard -= new zkemkeeper._IZKEMEvents_OnWriteCardEventHandler (axCZKEM1_OnWriteCard);
                //this.axCZKEM1.OnEmptyCard -= new zkemkeeper._IZKEMEvents_OnEmptyCardEventHandler (axCZKEM1_OnEmptyCard);

                bIsConnected = false;
                btnConnect.Text = "Connect";
                lblState.Text = "Current State:DisConnected";
                Cursor = Cursors.Default;
                return;
            }

            bIsConnected = axCZKEM1.Connect_Net (txtIP.Text, Convert.ToInt32 (txtPort.Text));
            if (bIsConnected == true)
            {
                txtIP.Enabled = txtPort.Enabled = false;
                btnConnect.Text = "DisConnect";
                btnConnect.Refresh ();
                lblState.Text = "Current State:Connected";
                iMachineNumber = 1;//In fact,when you are using the tcp/ip communication,this parameter will be ignored,that is any integer will all right.Here we use 1.

                if (axCZKEM1.RegEvent (iMachineNumber, 65535))//Here you can register the realtime events that you want to be triggered(the parameters 65535 means registering all)
                {
                    lbRTShow.Items.Clear ();
                    this.axCZKEM1.OnVerify += new zkemkeeper._IZKEMEvents_OnVerifyEventHandler (axCZKEM1_OnVerify);
                    //this.axCZKEM1.OnAttTransactionEx += new zkemkeeper._IZKEMEvents_OnAttTransactionExEventHandler (axCZKEM1_OnAttTransactionEx);
                    //this.axCZKEM1.OnNewUser += new zkemkeeper._IZKEMEvents_OnNewUserEventHandler (axCZKEM1_OnNewUser);
                    this.axCZKEM1.OnHIDNum += new zkemkeeper._IZKEMEvents_OnHIDNumEventHandler (axCZKEM1_OnHIDNum);
                    //this.axCZKEM1.OnWriteCard += new zkemkeeper._IZKEMEvents_OnWriteCardEventHandler (axCZKEM1_OnWriteCard);
                    //this.axCZKEM1.OnEmptyCard += new zkemkeeper._IZKEMEvents_OnEmptyCardEventHandler (axCZKEM1_OnEmptyCard);
                }
                checkExpireUsrs ();
            }
            else
            {
                axCZKEM1.GetLastError (ref idwErrorCode);
                MessageBox.Show ("Unable to connect the device,ErrorCode=" + idwErrorCode.ToString (), "Error");
            }
            Cursor = Cursors.Default;
        }


        private void btnRsConnect_Click(object sender, EventArgs e)
        {

        }

        private void btnUSBConnect_Click(object sender, EventArgs e)
        {

        }

        #region RealTime Events

        //When you have enrolled a new user,this event will be triggered.
        private void axCZKEM1_OnNewUser(int iEnrollNumber)
        {
            lbRTShow.Items.Clear ();
            lbRTShow.Items.Add ("RTEvent OnNewUser Has been Triggered...");
            lbRTShow.Items.Add ("...NewUserID=" + iEnrollNumber.ToString ());
        }

        //When you swipe a card to the device, this event will be triggered to show you the number of the card.
        private void axCZKEM1_OnHIDNum(int iCardNumber)
        {
            lbRTShow.Items.Clear ();
            DateTime time = new DateTime ();
            time = DateTime.Now;
            lbRTShow.Items.Add ("بطاقة مصرحة بالدخول ");
            lbRTShow.Items.Add ("رقم البطاقة=" + iCardNumber.ToString ());
            lbRTShow.Items.Add ("الساعة= " + DateTime.Now.ToString ());
            SqlParameter[] param = new SqlParameter[1];
            param[0] = new SqlParameter ("@ID", SqlDbType.BigInt);
            param[0].Value = iCardNumber;
            DataTable dtMonitor = new DataTable ();
            dtMonitor = dbClass.selectdata ("FilterUsrByID", param);

            if (dtMonitor.Rows.Count > 0)
            {
                CardMain form = new CardMain ();
                MemoryStream ms = new MemoryStream ((byte[]) dtMonitor.Rows[0]["UsrPic"]);
                pictureBox1.Image = new Bitmap (ms);
                form.pictureBoxUsr.Image = new Bitmap (ms);
                lbMonitorID.Text = dtMonitor.Rows[0]["UsrId"].ToString ();
                form.label_USRID.Text = dtMonitor.Rows[0]["UsrId"].ToString ();
                lbMonitorName.Text = dtMonitor.Rows[0]["UsrName"].ToString ();
                form.label_USRNAME.Text = dtMonitor.Rows[0]["UsrName"].ToString ();
                lbMonitorCard.Text = iCardNumber.ToString ();
                form.label_USRCARD.Text = iCardNumber.ToString ();
                lbMonitorAcc.Text = dtMonitor.Rows[0]["UsrAcc"].ToString ();
                form.label_USRACC.Text = dtMonitor.Rows[0]["UsrAcc"].ToString ();
                lbMonitorStartDate.Text = dtMonitor.Rows[0]["sDateAcc"].ToString ();
                form.label_USRBGIN.Text = Convert.ToDateTime (dtMonitor.Rows[0]["sDateAcc"]).ToShortDateString ();
                lbMonitorEndDate.Text = dtMonitor.Rows[0]["eDateAcc"].ToString ();
                form.label_USREND.Text = Convert.ToDateTime (dtMonitor.Rows[0]["eDateAcc"]).ToShortDateString ();
                lbMonitorTime.Text = time.ToString ();
                form.label_TIMEENTER.Text = time.ToString ();
                form.Show ();
                dvLog ();

            }


        }


        //When you have emptyed the Mifare card,this event will be triggered.
        private void axCZKEM1_OnEmptyCard(int iActionResult)
        {
            lbRTShow.Items.Clear ();
            lbRTShow.Items.Add ("RTEvent OnEmptyCard Has been Triggered...");
            if (iActionResult == 0)
            {
                lbRTShow.Items.Add ("...Empty Mifare Card OK");
            }
            else
            {
                lbRTShow.Items.Add ("...Empty Failed");
            }
        }

        //When you have written into the Mifare card ,this event will be triggered.
        private void axCZKEM1_OnWriteCard(int iEnrollNumber, int iActionResult, int iLength)
        {
            lbRTShow.Items.Clear ();
            lbRTShow.Items.Add ("RTEvent OnWriteCard Has been Triggered...");
            if (iActionResult == 0)
            {
                lbRTShow.Items.Add ("...Write Mifare Card OK");
                lbRTShow.Items.Add ("...EnrollNumber=" + iEnrollNumber.ToString ());
                lbRTShow.Items.Add ("...TmpLength=" + iLength.ToString ());
            }
            else
            {
                lbRTShow.Items.Add ("...Write Failed");
            }
        }

        //After you swipe your card to the device,this event will be triggered.
        //If your card passes the verification,the return value  will be user id, or else the value will be -1
        private void axCZKEM1_OnVerify(int iUserID)
        {

            lbRTShow.Items.Clear ();
            lbRTShow.Items.Add ("التحقق من البطاقة");
            if (iUserID != -1)
            {
                lbRTShow.Items.Add ("بطاقة مصرحة بالدخول " + iUserID.ToString ());
                lbRTShow.Items.Add ("رقم البطاقة=" + lbMonitorCard.Text.ToString ());
                lbRTShow.Items.Add ("الساعة= " + DateTime.Now.ToString ());


            }
            else
            {
                lbRTShow.Items.Add ("بطاقة غير فعالة");
                lbMonitorID.Text = lbMonitorCard.Text = lbMonitorAcc.Text = lbMonitorEndDate.Text = lbMonitorName.Text = lbMonitorStartDate.Text = lbMonitorTime.Text = "";
                pictureBox1.Image = null;
            }
        }

        //If your card passes the verification,this event will be triggered
        private void axCZKEM1_OnAttTransactionEx(string sEnrollNumber, int iIsInValid, int iAttState, int iVerifyMethod, int iYear, int iMonth, int iDay, int iHour, int iMinute, int iSecond, int iWorkCode)
        {
            lbRTShow.Items.Clear ();
            lbRTShow.Items.Add ("RTEvent OnAttTrasactionEx Has been Triggered,Verified OK");
            lbRTShow.Items.Add ("...UserID:" + sEnrollNumber);
            lbRTShow.Items.Add ("...isInvalid:" + iIsInValid.ToString ());
            //lbRTShow.Items.Add ("...attState:" + iAttState.ToString ());
            lbRTShow.Items.Add ("...VerifyMethod:" + iVerifyMethod.ToString ());
            //lbRTShow.Items.Add ("...Workcode:" + iWorkCode.ToString ());//the difference between the event OnAttTransaction and OnAttTransactionEx
            lbRTShow.Items.Add ("...Time:" + iYear.ToString () + "-" + iMonth.ToString () + "-" + iDay.ToString () + " " + iHour.ToString () + ":" + iMinute.ToString () + ":" + iSecond.ToString ());

            string sName = "";
            string sPassword = "";
            int iPrivilege = 0;
            bool bEnabled = false;
            string sCardnumber = "";

            while (axCZKEM1.SSR_GetUserInfo (iMachineNumber, sEnrollNumber, out sName, out sPassword, out iPrivilege, out bEnabled))//get user information from memory
            {
                if (axCZKEM1.GetStrCardNumber (out sCardnumber))//get the card number from the memory
                {
                    lbRTShow.Items.Add ("...Cardnumber:" + sCardnumber);
                    return;
                }
            }
        }

        //After function GetRTLog() is called ,RealTime Events will be triggered. 
        //When you are using these two functions, it will request data from the device forwardly.
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

        private void btnDeleteEnrollData_Click(object sender, EventArgs e)
        {
            if (dgvDeleUSr.Rows.Count > 0)
            {
                cbUserIDDE.Items.Clear ();
                int rowIndex = dgvDeleUSr.CurrentCell.RowIndex;
                var deletedID = dgvDeleUSr.Rows[rowIndex].Cells["UsrID"].Value.ToString ();
                cbUserIDDE.Items.Add (deletedID);
                cbUserIDDE.SelectedIndex = 0;
                if (dvDeleteUsr ())
                {
                    dbClass.deleteExistUsr (int.Parse (deletedID));
                    Cursor = Cursors.Default;
                    SelectAllUSr (dgvDeleUSr);
                    cbUserIDDE.Items.Clear ();

                }
                //MessageBox.Show (deletedID);
            }
        }

        private void UserMng_Enter(object sender, EventArgs e)
        {
            txtUserID.Text = dbClass.FetchUsrID ();
        }

        private void btnSetStrCardNumber_Click(object sender, EventArgs e)
        {
            if (txtCardnumber.Text == "" || txtName.Text == "" || PBusr.Image == null || (chIronAcc.Checked == false && chGymAcc.Checked == false))
            {
                MessageBox.Show ("يرجى اكمال ملئ الحقول");
            }
            else
            {
                if (btnSetStrCardNumber.Text == "حفظ" || btnSetStrCardNumber.Text == "تجديد اشتراك")
                {
                    if (dbClass.CheckCardNo (txtCardnumber.Text))
                    {
                        MessageBox.Show ("البطاقة مسجلة مسبقا في قاعدة البيانات");
                    }

                    else
                    {
                        if (dvSaveNewUsr ())
                        {
                            if (btnSetStrCardNumber.Text == "تجديد اشتراك")
                            {
                                SqlParameter[] param = new SqlParameter[1];
                                param[0] = new SqlParameter ("@Id", SqlDbType.BigInt);
                                param[0].Value = int.Parse (txtUserID.Text);
                                dbClass.ExecuteCommand ("deleteExpireUsr", param);

                            }
                            dbSaveNewUsr ();
                            clear ();
                            Cursor = Cursors.Default;
                        }
                    }
                }
                else
                {

                    cbUserIDDE.Items.Clear ();
                    cbUserIDDE.Items.Add (temproaryFlag);
                    cbUserIDDE.SelectedIndex = 0;
                    cbBackupDE.SelectedIndex = 12;
                    if (dvDeleteUsr ())
                    {
                        dbClass.deleteExistUsr (int.Parse (temproaryFlag));
                        if (dvSaveNewUsr ())
                        {
                            dbSaveNewUsr ();
                            clear ();
                            Cursor = Cursors.Default;
                        }
                    }

                }
            }

        }

        private void btnBrowsPic_Click(object sender, EventArgs e)
        {
            //OpenFileDialog browsPic = new OpenFileDialog ();

            //browsPic.InitialDirectory = @"C:\";
            //browsPic.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp)|*.jpg; *.jpeg; *.gif; *.bmp";
            //browsPic.Title = "Browse Text Files";
            //if (browsPic.ShowDialog () == DialogResult.OK)
            //{
            //    PBusr.Image = Image.FromFile (browsPic.FileName);
            //}
            if (btnBrowsPic.Text == "فتح الكاميرا")
            {
                videoCaptureDevice = new VideoCaptureDevice (filterInfoCollection[cboCmera.SelectedIndex].MonikerString);
                videoCaptureDevice.NewFrame += VideoCaptureDevice_NewFrame;
                videoCaptureDevice.Start ();
                btnBrowsPic.Text = "التقاط صورة";
            }
            else if (btnBrowsPic.Text == "التقاط صورة")
            {
                videoCaptureDevice.Stop ();
                btnBrowsPic.Text = "فتح الكاميرا";
            }
        }

        private void VideoCaptureDevice_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            PBusr.Image = (Bitmap) eventArgs.Frame.Clone ();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            clear ();
        }



        private void btnUsrEdit_Click(object sender, EventArgs e)
        {
            if (dgvEditUsr.Rows.Count > 0)
            {
                int rowIndex = dgvEditUsr.CurrentCell.RowIndex;
                string UsrId = dgvEditUsr.Rows[rowIndex].Cells["dgvEusrId"].Value.ToString ();
                string UsrName = dgvEditUsr.Rows[rowIndex].Cells["dgvEusrName"].Value.ToString ();
                string UsrCard = dgvEditUsr.Rows[rowIndex].Cells["dgvEusrCard"].Value.ToString ();
                string UsrAcc = dgvEditUsr.Rows[rowIndex].Cells["dgvEusrAcc"].Value.ToString ();
                DateTime sDateAcc = Convert.ToDateTime (dgvEditUsr.Rows[rowIndex].Cells["dgvEstartDate"].Value.ToString ());
                DateTime eDateAcc = Convert.ToDateTime (dgvEditUsr.Rows[rowIndex].Cells["dgvEendDate"].Value.ToString ());
                object value = dgvEditUsr.Rows[rowIndex].Cells["dgvEusrPic"].Value;
                MemoryStream ms = new MemoryStream ((byte[]) dgvEditUsr.Rows[rowIndex].Cells["dgvEusrPic"].Value);
                tabPagesMng.SelectedIndex = 1;
                txtUserID.Text = temproaryFlag = UsrId;
                txtName.Text = UsrName;
                txtCardnumber.Text = UsrCard;
                if (UsrAcc == "حديد")
                {
                    chIronAcc.Checked = true;
                }
                else if (UsrAcc == "رشاقة")
                {
                    chGymAcc.Checked = true;
                }
                else if (UsrAcc == "مشترك")
                {
                    chIronAcc.Checked = true;
                    chGymAcc.Checked = true;
                }
                PBusr.Image = new Bitmap (ms);
                dtpStartDate.Text = sDateAcc.ToShortDateString ();
                dtpEndDate.Text = eDateAcc.ToShortDateString ();
                btnSetStrCardNumber.Text = "تعديل";
                dtpStartDate.Enabled = false;
                dtpEndDate.Enabled = false;
            }
        }

        private void usrEdit_Enter(object sender, EventArgs e)
        {
            txtEditFilter.Text = "";
            SelectAllUSr (dgvEditUsr);
        }

        private void usrDelete_Enter(object sender, EventArgs e)
        {
            txbDelfilter.Text = "";
            SelectAllUSr (dgvDeleUSr);
        }

        private void btnClearLogs_Click(object sender, EventArgs e)
        {

            if (bIsConnected == false)
            {
                MessageBox.Show ("Please connect the device first", "Error");
                return;
            }
            int idwErrorCode = 0;


            axCZKEM1.EnableDevice (iMachineNumber, false);//disable the device
            if (axCZKEM1.ClearGLog (iMachineNumber))
            {
                axCZKEM1.RefreshData (iMachineNumber);//the data in the device should be refreshed
                MessageBox.Show ("تم مسح جميع السجلات من الجهاز", "نجاح");
            }
            else
            {
                axCZKEM1.GetLastError (ref idwErrorCode);
                MessageBox.Show ("Operation failed,ErrorCode=" + idwErrorCode.ToString (), "Error");
            }
            axCZKEM1.EnableDevice (iMachineNumber, true);//enable the device
        }

        private void usrRepo_Enter(object sender, EventArgs e)
        {

        }

        private void tabPagesMng_SelectedIndexChanged(object sender, EventArgs e)
        {


            switch (tabPagesMng.SelectedIndex)
            {
                case 1:
                    btnSetStrCardNumber.Text = "حفظ";
                    //DateTime time = new DateTime ();
                    //time = time.AddMonths (1);
                    //dtpEndDate.Text = time.ToShortDateString ();
                    clear ();
                    break;
                case 2:
                    break;
                case 3:
                    break;
                case 4:
                    DataTable dt3 = new DataTable ();
                    dt3 = dbClass.selectdata ("SelectDeviceLog", null);
                    if (dt3.Rows.Count > 0)
                    {
                        dgvLogDevice.DataSource = dt3;
                        gridView1.BestFitColumns ();
                    }
                    break;
                case 5:
                    dgvEndingAcc.DataSource = dbClass.selectdata ("allExpireUsrs", null);

                    break;
                default:
                    break;
            }
            //if (connctionFlag == 0)
            //{
            //    tabPagesMng.SelectedIndex = 0;
            //    MessageBox.Show ("الرجاء اتمام عملية الاتصال مع الجهاز ", "Message");
            //}
            //else
            //{
            //}
        }

        private void btnRenewAcc_Click(object sender, EventArgs e)
        {

            if (gridView2.RowCount > 0)
            {

                string UsrId = gridView2.GetRowCellValue (gridView2.FocusedRowHandle, gridView2.Columns[0]).ToString ();
                string UsrName = gridView2.GetRowCellValue (gridView2.FocusedRowHandle, gridView2.Columns[1]).ToString ();
                string UsrCard = gridView2.GetRowCellValue (gridView2.FocusedRowHandle, gridView2.Columns[2]).ToString ();
                string UsrAcc = gridView2.GetRowCellValue (gridView2.FocusedRowHandle, gridView2.Columns[3]).ToString ();
                DateTime sDateAcc = Convert.ToDateTime (gridView2.GetRowCellValue (gridView2.FocusedRowHandle, gridView2.Columns[4]).ToString ());
                DateTime eDateAcc = Convert.ToDateTime (gridView2.GetRowCellValue (gridView2.FocusedRowHandle, gridView2.Columns[5]).ToString ());

                //MemoryStream ms = new MemoryStream ((byte[]) gridView2.GetRowCellValue (gridView2.FocusedRowHandle, gridView2.Columns[0]).ToString ()
                tabPagesMng.SelectedIndex = 1;
                txtUserID.Text = temproaryFlag = UsrId;
                txtName.Text = UsrName;
                txtCardnumber.Text = UsrCard;
                txtUserID.Enabled = txtName.Enabled = txtCardnumber.Enabled = false;
                if (UsrAcc == "حديد")
                {
                    chIronAcc.Checked = true;
                }
                else if (UsrAcc == "رشاقة")
                {
                    chGymAcc.Checked = true;
                }
                else if (UsrAcc == "مشترك")
                {
                    chIronAcc.Checked = true;
                    chGymAcc.Checked = true;
                }
                //PBusr.Image = new Bitmap (ms);
                //PBusr.Enabled = false;
                dtpStartDate.Text = sDateAcc.ToShortDateString ();
                dtpEndDate.Text = eDateAcc.ToShortDateString ();
                btnSetStrCardNumber.Text = "تجديد اشتراك";

            }
        }

        private void btnLogDateSearch_Click(object sender, EventArgs e)
        {
            SqlParameter[] param = new SqlParameter[2];
            param[0] = new SqlParameter ("@sLogDate", SqlDbType.NVarChar, 50);
            param[1] = new SqlParameter ("@eLogDate", SqlDbType.NVarChar, 50);
            param[0].Value = dtpSlogDate.Text + " 00:00:00";
            param[1].Value = dtpElogDate.Text + " 23:59:00";
            dgvLogDevice.DataSource = dbClass.selectdata ("SelectLogBetweenDates", param);
        }

        private void dtpStartDate_Leave(object sender, EventArgs e)
        {
            if (dtpStartDate.Value.Date < DateTime.Now.Date)
            {
                dtpStartDate.Focus ();
                MessageBox.Show ("يجب ادخال تاريخ صحيح", "Message");
                dtpStartDate.Value = DateTime.Now.Date;
            }
        }

        private void dtpEndDate_Leave(object sender, EventArgs e)
        {
            if (dtpEndDate.Value.Date < DateTime.Now.Date)
            {
                MessageBox.Show ("يجب ادخال تاريخ صحيح", "Message");
                dtpEndDate.Value = DateTime.Now.Date;
                dtpEndDate.Focus ();
            }
        }

        private void txtEditFilter_TextChanged(object sender, EventArgs e)
        {
            this.SearchUsr (this.dgvEditUsr, txtEditFilter.Text);
        }

        private void txbDelfilter_TextChanged(object sender, EventArgs e)
        {
            this.SearchUsr (this.dgvDeleUSr, txbDelfilter.Text);
        }

        private void txtLogFilter_TextChanged(object sender, EventArgs e)
        {
            DataTable dt = new DataTable ();
            SqlParameter[] param = new SqlParameter[1];
            param[0] = new SqlParameter ("@filter", SqlDbType.NVarChar, 50);
            param[0].Value = txtLogFilter.Text;
            dt = dbClass.selectdata ("SelectLogByFilter", param);
            dgvLogDevice.DataSource = dt;
        }

        private void txtEndingFilter_TextChanged(object sender, EventArgs e)
        {
            DataTable dt = new DataTable ();
            SqlParameter[] param = new SqlParameter[1];
            param[0] = new SqlParameter ("@Filter", SqlDbType.NVarChar, 50);
            param[0].Value = txtEndingFilter.Text;
            dt = dbClass.selectdata ("FilterUsrExpire", param);
            dgvEndingAcc.DataSource = dt;
        }

        private void txtCardnumber_KeyPress(object sender, KeyPressEventArgs e)
        {
            base.OnKeyPress (e);
            if (!char.IsControl (e.KeyChar) && !char.IsDigit (e.KeyChar))
                e.Handled = true;
        }

        private void txtName_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (Char.IsDigit (e.KeyChar))
                e.Handled = true;
        }

        private void btnLOG_Click(object sender, EventArgs e)
        {
            printableComponentLink1.CreateDocument ();
            printableComponentLink1.ShowPreview ();
            //RPT.LOGRPT myReport = new RPT.LOGRPT ();
            //myReport.Database.Tables["ExpireUsrs"].SetDataSource (dgvLogDevice.DataSource);
            //RPT.GYMRPT myform = new RPT.GYMRPT ();
            //myform.ReportViwer.ReportSource = myReport;
            //myform.ShowDialog ();
        }



        private void btn_PrintExpire_Click(object sender, EventArgs e)
        {
            printableComponentLink2.CreateDocument ();
            printableComponentLink2.ShowPreview ();
        }
        FilterInfoCollection filterInfoCollection;
        VideoCaptureDevice videoCaptureDevice;
        private void MainForm_Load(object sender, EventArgs e)
        {
            filterInfoCollection = new FilterInfoCollection (FilterCategory.VideoInputDevice);
            foreach (FilterInfo item in filterInfoCollection)

                cboCmera.Items.Add (item.Name);
            cboCmera.SelectedIndex = 1;
            videoCaptureDevice = new VideoCaptureDevice ();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (videoCaptureDevice.IsRunning == true)
            {
                videoCaptureDevice.Stop ();
            }
        }
    }
}
