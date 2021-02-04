using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cdrf.task {
    class Update : Tasks {
        public override object run(object obj) {
            Connection c = new Connection(Connection.getConnectionString(obj.GetType()));
            bool r = c.Update(obj);
            c.delete();
            if (r) {
                if (r) {
                    string[] dat = obj.GetType().Namespace.Split('.');
                    string name = obj.GetType().Name;
                    Cache.getInstance().addData(dat[dat.Length - 1],name,obj);
                }
            }
            return r;
        }
    }
}
