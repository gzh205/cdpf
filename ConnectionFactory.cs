using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace cdrf
{
    public class ConnectionFactory
    {
        /// <summary>
        /// Connection列表用于多线程存取操作
        /// </summary>
        public static List<Connection> connections;
        /// <summary>
        /// 根据连接字符串创建一个新的Connection对象
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <returns>返回一个Connection对象</returns>

        public static Connection getConnection(string connectionString)
        {
            if (connections.Count == 0)
            {
                queueNum = new Semaphore(0, 32767);
            }
            SqlConnection conn = new SqlConnection(connectionString);
            foreach(Connection c in connections)
            {
                if(c.sqlConn == conn)
                {
                    return c;
                }
            }
            Connection connection = new Connection(conn);
            ConnectionFactory.connections.Add(connection);
            return connection;
        }
    }
}
