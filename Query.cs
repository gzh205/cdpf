using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cdrf
{
    public class Query
    {
        public SqlConnection sqlConn { get; set; }
        public SqlCommand cmd { get; set; }
        public string sql { get; set; }

        public SqlDataReader
    }
}
