﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
namespace cdrf
{
    class Connection
    {
        /// <summary>
        /// 数据库连接池
        /// </summary>
        private static Dictionary<string,string> connections = new Dictionary<string,string>();
        /// <summary>
        /// 用于处理线程池的信号量
        /// </summary>
        private static Semaphore queueNum = new Semaphore(0,32767);
        /// <summary>
        /// 多线程处理
        /// </summary>
        private static Queue<Tasks> queue = new Queue<Tasks>();
        /// <summary>
        /// 正则表达式常量，用于匹配Database={数据库名}
        /// </summary>
        private const string pattern = @"(?<=(Database|database)[\s]*=[\s]*)[^;]+(?=;)";
        /// <summary>
        /// 根据连接字符串创建一个新的Connection对象
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <returns>返回一个Connection对象</returns>
        public static void addConnection(string connectionString) {
            SqlConnection conn = new SqlConnection(connectionString);
            Match mc = Regex.Match(connectionString,pattern);
            string res = mc.Value;
            connections.Add(res,connectionString);
        }
        /// <summary>
        /// 根据类型名获取连接
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string getConnectionString(Type type) {
            string[] dat = type.Namespace.Split('.');
            string ns = dat[dat.Length - 1];
            string res;
            try {
                res = connections[ns];
            } catch (KeyNotFoundException) {
                throw new SqlException("找不到与该类型对应命名空间相同的连接字符串索引"+dat[dat.Length-1]);
            }
            return res;
        }
        public string connectionstring { get; set; }
        /// <summary>
        /// 构造函数，不建议直接调用
        /// </summary>
        /// <param name="sqlConn"></param>
        public Connection(string connectingstring)
        {
            this.connectionstring = connectionstring;
        }
        /// <summary>
        /// 删除对象
        /// </summary>
        public void delete() {
            this.connectionstring = null;
        }
        /// <summary>
        /// 自定义select查询的where语句部分,其中select与from均可省略,如不省略,该方法也会自动忽略
        /// </summary>
        /// <typeparam name="T">继承自Table类的数据库表类型</typeparam>
        /// <param name="where">自定义的where语句</param>
        /// <returns>返回所有符合where语句限定条件的记录</returns>
        public T[] SelectSome<T>(string where)
        {
            SqlConnection sqlConn = new SqlConnection(connectionstring);
            SqlCommand cmd = sqlConn.CreateCommand();
            if (where == null)
                where = "";
            if (where.Contains(where) && !where.Contains("'where'") && !where.Contains("\"where\""))
            {
                where = Regex.Replace(where, ".*where", "where");
            }
            List<T> table = new List<T>();
            FieldInfo[] fields = typeof(T).GetFields();
            PropertyInfo[] properties = typeof(T).GetProperties();
            string sql = "select ";
            string selectStr = "";
            for (int i = 0; i < fields.Length; i++)
                selectStr += ","+fields[i].Name;
            for (int i = 0; i < properties.Length; i++)
                selectStr += "," + properties[i].Name;
            sql += selectStr.Substring(1) + " from " + typeof(T).Name + where;
            cmd.CommandText = sql;
            sqlConn.Open();
            SqlDataReader sdr = cmd.ExecuteReader();
            fields = null;
            properties = null;
            while(sdr.Read())
            {
                T t = System.Activator.CreateInstance<T>();
                FieldInfo[] tfields = t.GetType().GetFields();
                PropertyInfo[] tproperties = t.GetType().GetProperties();
                for (int i = 0; i < tfields.Length; i++) 
                {
                    tfields[i].SetValue(t, sdr[tfields[i].Name]);
                }
                for(int i = 0; i < tproperties.Length; i++)
                {
                    tproperties[i].SetValue(t, sdr[tproperties[i].Name]);
                }
                table.Add(t);
            }
            sqlConn.Close();
            return table.ToArray();
        }
        /// <summary>
        /// 查询数据库中的记录
        /// </summary>
        /// <param name="table">某条记录的对象,继承自Table,只要[PrimaryKey]有值即可,其余成员会自动填充完成</param>
        public void Select(object table)
        {
            SqlConnection sqlConn = new SqlConnection(connectionstring);
            SqlCommand cmd = sqlConn.CreateCommand();
            FieldInfo[] fields;
            PropertyInfo[] properties;
            LinkedList<Node>[] lists = this.getFieldsAndAttributes(table,out fields,out properties);
            string sql = "select ";
            string selectStr = "";
            string whereStr = "";
            foreach(Node node in lists[0])
            {
                whereStr += " and " + node.value + "=" + node.name;
            }
            foreach(Node node in lists[1])
            {
                selectStr += "," + node.name;
            }
            whereStr = whereStr.Substring(5);
            selectStr = selectStr.Substring(1);
            sql += selectStr + " from " + table.GetType().Name + " where " + whereStr;
            cmd.CommandText = sql;
            sqlConn.Open();
            SqlDataReader sdr = cmd.ExecuteReader();
            sdr.Read();
            //int index = 0;
            for(int i = 0; i < fields.Length; i++)
            {
                fields[i].SetValue(table, sdr[fields[i].Name]);
            }
            for(int i = 0; i < properties.Length; i++)
            {
                properties[i].SetValue(table, sdr[properties[i].Name]);
            }
            sqlConn.Close();
        }
        /// <summary>
        /// 修改数据库表中的记录
        /// </summary>
        /// <param name="table"></param>
        /// <returns>成功返回true,失败返回false</returns>
        public bool Update(object table)
        {
            SqlConnection sqlConn = new SqlConnection(connectionstring);
            SqlCommand cmd = sqlConn.CreateCommand();
            LinkedList<Node>[] lists = this.getFieldsAndAttributes(table);
            string sql = "update " + table.GetType().Name + " set ";
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
            sql += setStr.Substring(1) + " where " + whereStr.Substring(5);
            cmd.CommandText = sql;
            sqlConn.Open();
            int n = cmd.ExecuteNonQuery();
            sqlConn.Close();
            return n > 0;
        }
        /// <summary>
        /// 从数据库表中删除一条记录
        /// </summary>
        /// <param name="table">某条记录的对象,继承自Table,只要[PrimaryKey]正确即可,其余成员均可为null或任意值</param>
        /// <returns>成功返回true,失败返回false</returns>
        public bool Delete(object table)
        {
            SqlConnection sqlConn = new SqlConnection(connectionstring);
            SqlCommand cmd = sqlConn.CreateCommand();
            LinkedList<Node>[] lists = this.getFieldsAndAttributes(table);
            string sql = "delete from " + table.GetType().Name + " where ";
            string whereString = "";
            foreach (Node node in lists[0])
                whereString += " and " + node.name + "=" + node.value;
            sql += whereString.Substring(5);
            cmd.CommandText = sql;
            sqlConn.Open();
            int n = cmd.ExecuteNonQuery();
            sqlConn.Close();
            return n > 0;
        }
        /// <summary>
        /// 将一条记录插入数据库的表中
        /// </summary>
        /// <param name="table">某条记录的对象,继承自Table,[PrimaryKey]标记表示该成员为主关键字</param>
        /// <returns>插入成功返回true,失败返回false</returns>
        public bool Insert(object table)
        {
            SqlConnection sqlConn = new SqlConnection(connectionstring);
            SqlCommand cmd = sqlConn.CreateCommand();
            LinkedList<Node>[] lists = this.getFieldsAndAttributes(table);
            string sql = "insert into " + table.GetType().Name;
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
        private LinkedList<Node>[] getFieldsAndAttributes(object table)
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
        /// <summary>
        /// 查找table表中所有的成员并且输出它们的ID和值,并且获得他们的对象
        /// </summary>
        /// <param name="table"></param>
        /// <param name="fInfos"></param>
        /// <param name="pInfos"></param>
        /// <returns></returns>
        private LinkedList<Node>[] getFieldsAndAttributes(object table,out FieldInfo[] fInfos,out PropertyInfo[] pInfos)
        {
            List<FieldInfo> datField = new List<FieldInfo>();
            List<PropertyInfo> datProperty = new List<PropertyInfo>();
            LinkedList<Node>[] lists = new LinkedList<Node>[2];
            for (int i = 0; i < lists.Length; i++)
                lists[i] = new LinkedList<Node>();
            FieldInfo[] fields = table.GetType().GetFields();
            PropertyInfo[] properties = table.GetType().GetProperties();
            foreach (FieldInfo field in fields)
            {
                lists[1].AddLast(new Node(field.Name, null));
                datField.Add(field);
                if (field.GetCustomAttribute(typeof(PrimaryKey)) != null)
                {
                    string value = field.GetValue(table).ToString();
                    if (field.FieldType.Name == "String" || field.FieldType.Name == "DateTime")
                        value = "'" + value + "'";
                    lists[0].AddLast(new Node(field.Name, value));
                }
            }
            foreach (PropertyInfo property in properties)
            {
                lists[1].AddLast(new Node(property.Name, null));
                datProperty.Add(property);
                if (property.GetCustomAttribute(typeof(PrimaryKey)) != null)
                {
                    string value = property.GetValue(table).ToString();
                    if (property.PropertyType.Name == "String" || property.PropertyType.Name == "DateTime")
                        value = "'" + value + "'";
                    lists[0].AddLast(new Node(property.Name, value));
                }
            }
            fInfos = datField.ToArray();
            pInfos = datProperty.ToArray();
            return lists;
        }
    }
}
