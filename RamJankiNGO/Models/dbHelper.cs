using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Extensions.Configuration;

namespace RamJankiNGO.Models
{
    public class dbHelper
    {
        private readonly string _connectionString;
        public dbHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("con");
        }

        public int ExecuteNonQueryProc(string cmdText, SqlParameter[] prms)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    if (prms != null)
                    {
                        cmd.Parameters.AddRange(prms);
                    }
                    conn.Open();
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        public DataTable ExecProcDataTable(string proName, SqlParameter[] param)
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(proName, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    if (param != null)
                    {
                        cmd.Parameters.AddRange(param);
                    }

                    using (SqlDataAdapter adp = new SqlDataAdapter(cmd))
                    {
                        adp.Fill(dt);
                    }
                }
            }
            return dt;
        }

        public DataSet ExecProcDataSet(string proName, SqlParameter[] param)
        {
            DataSet ds = new DataSet();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(proName, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    if (param != null)
                    {
                        cmd.Parameters.AddRange(param);
                    }

                    using (SqlDataAdapter adp = new SqlDataAdapter(cmd))
                    {
                        adp.Fill(ds);
                    }
                }
            }
            return ds;
        }

        public DataTable ExecAdaptorDataTable(string query)
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    using (SqlDataAdapter adp = new SqlDataAdapter(cmd))
                    {
                        adp.Fill(dt);
                    }
                }
            }
            return dt;
        }

        public int ExecuteNonQuery(string query)
        {
            return ExecuteNonQuery(query, null);
        }

        public int ExecuteNonQuery(string query, SqlParameter[]? prms)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    if (prms != null) cmd.Parameters.AddRange(prms);
                    conn.Open();
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        public DataTable ExecDataTable(string query, SqlParameter[]? prms)
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    if (prms != null) cmd.Parameters.AddRange(prms);
                    using (SqlDataAdapter adp = new SqlDataAdapter(cmd))
                    {
                        adp.Fill(dt);
                    }
                }
            }
            return dt;
        }

        public object ExecuteScalar(string query)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    conn.Open();
                    return cmd.ExecuteScalar();
                }
            }
        }
    }
}
