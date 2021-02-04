using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cdrf {
    class LRUList<T> : LinkedList<T>{
        public T this[int index] {
            get {
                if (index >= this.Count)
                    return default(T);
                Enumerator e = this.GetEnumerator();
                for (int i=0;i<=index;i++) {
                    e.MoveNext();
                }
                return e.Current;
            }
        }
    }
}