using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Text;

namespace Card
{
    class dataBaseClass
    {
        #region dbParam
        //SqlConnection Con = new SqlConnection("Data Source=localhost;Initial Catalog=SuperMarket;Persist Security Info=True;User ID=sa;Password=123");
        SqlConnection Con = new SqlConnection ("Data Source=localhost;Initial Catalog=Gym;Persist Security Info=True;User ID=sa;Password=123");
        DataTable dt = new DataTable ();
        public static int x = 0;

        #endregion

        #region Connection 
        //method to open connection
        public void DBopenConnection()
        {
            if (Con.State != ConnectionState.Open)
            {
                Con.Open ();
            }
        }

        //method to close connection
        public void DBcloseConnection()
        {
            if (Con.State == ConnectionState.Open)
            {
                Con.Close ();
            }
        }
        #endregion


        //method to select data from DB
        public DataTable selectdata(string storedProcedure, SqlParameter[] parameter)
        {
            SqlCommand cmd = new SqlCommand ();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = storedProcedure;
            cmd.Connection = Con;

            if (parameter != null)
            {
                for (int i = 0; i < parameter.Length; i++)
                {
                    cmd.Parameters.Add (parameter[i]);
                }
            }
            SqlDataAdapter sda = new SqlDataAdapter (cmd);
            //DataTable dt = new DataTable ();
            dt.Rows.Clear ();
            sda.Fill (dt);

            return dt;
        }
        //method to insert, update and delete DATA from DB
        public void ExecuteCommand(string StoredProcedure, SqlParameter[] parameter)
        {
            SqlCommand cmd = new SqlCommand ();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = StoredProcedure;
            cmd.Connection = Con;
            if (parameter != null)
            {
                cmd.Parameters.AddRange (parameter);
            }
            Con.Open ();
            cmd.ExecuteNonQuery ();
            cmd.Parameters.Clear ();
            Con.Close ();

        }

        //Check wherethere Card numbser is rgeister in device before
        public bool CheckCardNo(string text)
        {
            //var dt = new DataTable ();
            SqlParameter[] param = new SqlParameter[1];
            param[0] = new SqlParameter ("@CardNo", SqlDbType.BigInt);
            param[0].Value = int.Parse (text);
            dt.Rows.Clear ();
            dt = selectdata ("CheckCardNo", param);
            if (dt.Rows.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //Get UsrId from DB 
        public string FetchUsrID()
        {
            dt.Rows.Clear ();
            //var dt1 = new DataTable ();
            dt = selectdata ("FetchUsrID", null);
            if (dt.Rows.Count > 0)
            {
                return dt.Rows[0]["UsrId"].ToString ();
            }
            else
            {
                return null;
            }
        }

        //filter Info using Name or CardNo
        public DataTable FilterNameCard(string filter)
        {
            //var dt = new DataTable ();
            SqlParameter[] param = new SqlParameter[1];
            param[0] = new SqlParameter ("@filter", SqlDbType.NVarChar, 50);
            param[0].Value = filter;
            dt.Rows.Clear ();
            dt = selectdata ("filterUsrsByNameORcardNo", param);
            if (dt.Rows.Count > 0)
            {
                return dt;
            }
            else
            {
                return null;
            }
        }

        public void deleteExistUsr(int PreNo)
        {
            SqlParameter[] param = new SqlParameter[1];
            param[0] = new SqlParameter ("@PreCard", SqlDbType.BigInt);
            param[0].Value = PreNo;
            ExecuteCommand ("DeleteUsr", param);
        }



    }


}

