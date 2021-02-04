using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cdrf.task {
    class Delete : Tasks {
        public override object run(object obj) {
            Connection c = new Connection(Connection.getConnectionString(obj.GetType()));
            bool r = c.Delete(obj);
            c.delete();
            if (r) {
                string[] dat = obj.GetType().Namespace.Split('.');
                string name = obj.GetType().Name;
                Cache.getInstance().removeData(dat[dat.Length - 1],name,obj);
            }
            return r;
        }
    }
}
