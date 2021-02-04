using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cdrf {
    public class SqlException :Exception {
        public SqlException(string msg) : base(msg) {

        }
    }
}
