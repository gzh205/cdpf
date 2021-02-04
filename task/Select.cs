using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cdrf.task {
    class Select :Tasks{
        public override object run(object obj) {
            string[] dat = obj.GetType().Namespace.Split('.');
            string name = obj.GetType().Name;
            object result = Cache.getInstance().getData(dat[dat.Length-1],name,obj);
            if (result == null) {
                Connection c = new Connection(Connection.getConnectionString(obj.GetType()));
                c.Select(obj);
                c.delete();
                result = obj;
            }
            return Tasks.CpoyObject(result);
        }
    }
}
