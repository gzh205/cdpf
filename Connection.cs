using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Reflection;

namespace cdrf
{
    public class Connection
    {
        protected SqlConnection sqlConn { get; set; }
        protected SqlCommand cmd { get; set; }
        public string sql { get; protected set; }
        /// <summary>
        /// 根据连接字符串创建一个新的Connection对象
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <returns>返回一个Connection对象</returns>
        public static Connection getSession(string connectionString)
        {
            Connection connection = new Connection();
            connection.sqlConn = new SqlConnection(connectionString);
            connection.cmd = connection.sqlConn.CreateCommand();
            return connection;
        }
        public bool Update(Table table)
        {
            LinkedList<Node>[] lists = this.getFieldsAndAttributes(table);
            this.sql = "update " + table.GetType().Name + " set ";
            string setStr = "";
            string whereStr = "";
            foreach(Node node in lists[1])
            {
                setStr += "," + node.name + "=" + node.value;
            }
            foreach(Node node in lists[0])
            {
                whereStr += " and " + node.name + "=" + node.value;
            }
            this.sql += setStr.Substring(1) + " where " + whereStr.Substring(5);
            this.cmd.CommandText = this.sql;
            this.sqlConn.Open();
            int n = this.cmd.ExecuteNonQuery();
            this.sqlConn.Close();
            return n > 0;
        }
        /// <summary>
        /// 从数据库表中删除一条记录
        /// </summary>
        /// <param name="table">某条记录的对象,继承自Table,只要[PrimaryKey]正确即可,其余成员均可为null或任意值</param>
        /// <returns>成功返回true,失败返回false</returns>
        public bool Delete(Table table)
        {
            LinkedList<Node>[] lists = this.getFieldsAndAttributes(table);
            this.sql = "delete from " + table.GetType().Name + " where ";
            string whereString = "";
            foreach (Node node in lists[0])
                whereString += " and " + node.name + "=" + node.value;
            this.sql += whereString.Substring(5);
            this.cmd.CommandText = sql;
            this.sqlConn.Open();
            int n = this.cmd.ExecuteNonQuery();
            this.sqlConn.Close();
            return n > 0;
        }
        /// <summary>
        /// 将一条记录插入数据库的表中
        /// </summary>
        /// <param name="table">某条记录的对象,继承自Table,[PrimaryKey]标记表示该成员为主关键字</param>
        /// <returns>插入成功返回true,失败返回false</returns>
        public bool Insert(Table table)
        {
            LinkedList<Node>[] lists = this.getFieldsAndAttributes(table);
            this.sql = "insert into " + table.GetType().Name;
            string name = "(";
            string value = "(";
            for (int i = 0; i < lists.Length; i++)
            {
                foreach (Node node in lists[i])
                {
                    name += node.name + ",";
                    value += node.value + ",";
                }
            }
            name = name.Remove(name.Length - 1) + ")";
            value = value.Remove(value.Length - 1) + ")";
            sql += name + " values" + value;
            cmd.CommandText = sql;
            sqlConn.Open();
            int n = cmd.ExecuteNonQuery();
            sqlConn.Close();
            return n > 0;
        }
        /// <summary>
        /// 查找table表中所有的成员并且输出它们的ID和值
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        private LinkedList<Node>[] getFieldsAndAttributes(Table table)
        {
            LinkedList<Node>[] lists = new LinkedList<Node>[2];
            for (int i = 0; i < lists.Length; i++)
                lists[i] = new LinkedList<Node>();
            FieldInfo[] fields = table.GetType().GetFields();
            PropertyInfo[] properties = table.GetType().GetProperties();
            foreach(FieldInfo field in fields)
            {
                string value = field.GetValue(table).ToString();
                if (field.FieldType.Name == "String" || field.FieldType.Name == "DateTime")
                    value = "'" + value + "'";
                if (field.GetCustomAttribute(typeof(PrimaryKey)) == null)
                    lists[1].AddLast(new Node(field.Name,value));
                else
                    lists[0].AddLast(new Node(field.Name, value));
            }
            foreach(PropertyInfo property in properties)
            {
                string value = property.GetValue(table).ToString();
                if (property.PropertyType.Name == "String" || property.PropertyType.Name == "DateTime")
                    value = "'" + value + "'";
                if (property.GetCustomAttribute(typeof(PrimaryKey)) == null)
                    lists[1].AddLast(new Node(property.Name, value));
                else
                    lists[0].AddLast(new Node(property.Name, value));
            }
            return lists;
        }
    }
}
