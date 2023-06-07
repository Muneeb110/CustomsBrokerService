using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerBrokerService
{
    class DBManager
    {
        public OrderInstructions GetOrderInstructions()
        {
            OrderInstructions orderInstructions = null;
            try
            {
                var con = ConfigurationManager.AppSettings["dbConnectionString"].ToString();

                
                using (SqlConnection myConnection = new SqlConnection(con))
                {
                    string oString = "SELECT * FROM [SPRING].[dbo].[vw_export2aeb_addBrokerInstructionEvents] order by localReference asc;";
                    SqlCommand oCmd = new SqlCommand(oString, myConnection);
                    myConnection.Open();
                    using (SqlDataReader oReader = oCmd.ExecuteReader())
                    {
                        while (oReader.Read())
                        {
                            orderInstructions = new OrderInstructions();
                            orderInstructions.createDate = (DateTime)oReader["createDate"];
                            orderInstructions.timezoneCreateDate = oReader["timezoneCreateDate"].ToString();
                            orderInstructions.actualDate = (DateTime)oReader["actualDate"];
                            orderInstructions.timezoneActualDate = oReader["timezoneActualDate"].ToString();
                            orderInstructions.identCode = oReader["identCode"].ToString();
                            orderInstructions.localReference = oReader["localReference"].ToString();
                            orderInstructions.typeReference = oReader["typeReference"].ToString();
                            orderInstructions.valueReference = oReader["valueReference"].ToString();
                            orderInstructions.additionalInfo = oReader["additionalInfo"].ToString();
                        }

                        myConnection.Close();
                    }
                }
                return orderInstructions;
            }
            catch(Exception ex)
            {
                throw (ex);
            }
        }

        public void UpdateCommericalTable(string localReference, string status)
        {
            try
            {
                var con = ConfigurationManager.AppSettings["dbConnectionString"].ToString();
                using (SqlConnection myConnection = new SqlConnection(con))
                {
                    SqlCommand SqlComm = new SqlCommand("update commercial set status = @status where localReference = @localReference", myConnection);
                    SqlComm.Parameters.AddWithValue("@localReference", localReference);
                    SqlComm.Parameters.AddWithValue("@status", status);

                    myConnection.Open();
                    int i  = SqlComm.ExecuteNonQuery();

                    myConnection.Close();
                }

            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }

        public void InsertInLogTable(string localReference, string status, string message,string table)
        {
            try
            {
                var con = ConfigurationManager.AppSettings["dbConnectionString"].ToString();
                using (SqlConnection myConnection = new SqlConnection(con))
                {
                    SqlCommand SqlComm = new SqlCommand("insert into dbo.History (dateTime, [key], [table], status, message)values(CURRENT_TIMESTAMP, @key,@table,@status,@message)", myConnection);
                    SqlComm.Parameters.AddWithValue("@key", localReference);
                    SqlComm.Parameters.AddWithValue("@table", table);
                    SqlComm.Parameters.AddWithValue("@status", status);
                    SqlComm.Parameters.AddWithValue("@message", message);

                    myConnection.Open();
                    SqlComm.ExecuteNonQuery();

                    myConnection.Close();
                }

            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }
    }

    public class OrderInstructions
    {
        public DateTime createDate { get; set; }
        public string timezoneCreateDate { get; set; }
        public string localReference { get; set; }
        public string identCode { get; set; }
        public DateTime actualDate { get; set; }
        public string timezoneActualDate { get; set; }
        public string typeReference { get; set; }
        public string valueReference { get; set; }
        public string additionalInfo { get; set; }
    }
}
