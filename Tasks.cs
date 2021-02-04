using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Threading;
using cdrf.task;
using System.IO;
using System.Reflection;

namespace cdrf {
    abstract class Tasks {
        public Tasks() {
        }
        public Tasks(object table) {
            this.table = table;
        }
        public void init(string[] connectionStrings,int threadNum) {
            //获取所有的连接字符串并逐一添加
            foreach (string s in connectionStrings) {
                Connection.addConnection(s);
            }
            tasksNum = new Semaphore(0,threadNum);
            queuereader = new Semaphore(1,1);
        }
        //下面是多线程处理逻辑
        private static Queue<Tasks> tasks = new Queue<Tasks>();
        private static Semaphore tasksNum;
        private static Semaphore queuereader;
        private static int threadnum;
        public object table;
        public static void insertQueue(Tasks task) {
            tasks.Enqueue(task);
            tasksNum.Release();
        }
        public void start() {
            Thread[] threads = new Thread[threadnum];
            for(int i = 0;i < threadnum;i++) {
                threads[i] = new Thread(threadproc);
                threads[i].Start();
            }
        }
        private void threadproc() {
            //获取一个任务
            queuereader.WaitOne();
            tasksNum.WaitOne();
            Tasks task = tasks.Dequeue();
            queuereader.Release();
            //处理任务
            task.run(table);
        }
        public abstract object run(object obj);
        public static object CpoyObject(object o) {
            Type t = o.GetType();
            object result = Activator.CreateInstance(t);
            PropertyInfo[] properties = t.GetProperties();
            FieldInfo[] fields = t.GetFields();
            foreach(PropertyInfo i in properties) {
                i.SetValue(result,i.GetValue(o));
            }
            foreach(FieldInfo i in fields) {
                i.SetValue(result,i.GetValue(o));
            }
            return result;
        }
    }
}