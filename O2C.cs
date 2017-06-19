using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;


namespace SFTP_O2C
{
    class O2C
    {
        private string _connectionString;
        private SqlDataAdapter _adapter = null;

        public O2C(string connectionString)
        {
            // Set connctionstring
            _connectionString = connectionString;

            // Config adapter
            ConfigureAdapter(out _adapter);

            // Insert, update etc
            //var builder = new SqlCommandBuilder(_adapter);
        }
        private void ConfigureAdapter(out SqlDataAdapter adapter)
        {
            adapter = new SqlDataAdapter(@"
                SELECT cp.cnt_email AS Email,
                        cp.FullName,
                        c.cmp_name AS Relatienaam,
	                    LTRIM(RTRIM(c.debcode)) AS Debcode,
	                    CONCAT('\\nasheadclst\FTP Server\FTP Klanten\', dbo.FixPath(c.cmp_name), '_', LTRIM(RTRIM(c.debcode)), '\') AS SFTP_Path
                        --CAST(CSNobEntPortalAccess AS bit) AS Access
                FROM    CSNobEntCicntp pa
                INNER JOIN cicntp cp ON pa.LinkID = cp.cnt_id
                INNER JOIN cicmpy c ON cp.cmp_wwn = c.cmp_wwn
                WHERE   cp.cnt_email IS NOT NULL
                AND c.debcode IS NOT NULL
                AND CAST(CSNobEntPortalAccess AS bit) = 1
                AND cp.cnt_email <> 'j.bijlsma@rotaform.nl'"
                , _connectionString);
        }

        public DataTable GetSanddwebUsers()
        {
            DataTable usr = new DataTable("O2CUsers");
            _adapter.Fill(usr);
            return usr;
        }
        public List<string> GetUsersAsList(DataTable dt)
        {
            return dt.Rows.OfType<DataRow>().Select(dr => dr.Field<string>(0)).ToList();
        }
    }
}
