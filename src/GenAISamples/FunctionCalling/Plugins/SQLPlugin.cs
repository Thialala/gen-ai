using System;
using System.ComponentModel;
using Microsoft.Data.SqlClient;
using Microsoft.SemanticKernel;
using System.Threading.Tasks;
namespace FunctionCalling.Plugins
{
    public class SQLPlugin
    {
        private readonly string _connectionString;

        public SQLPlugin(string connectionString)
        {
            _connectionString = connectionString;
        }


        [KernelFunction, Description("Obtain the table names in SunuElection, which contains Candidats, Bureaux de Vote, PVs and Elections results. Always run this before running other queries instead of assuming the user mentioned the correct name.")]
        public string GetTables()
        {
            Console.WriteLine($"Getting tables...");
            return QueryAsCSV($"SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES;");
        }


        [KernelFunction, Description("Obtain the database schema for a table in SunuElection.")]
        public string GetSchema(
            [Description("The table to get the schema for. Do not include the schema name.")] string tableName
        )
        {
            Console.WriteLine($"Getting schema for table \"{tableName}\"...");
            return QueryAsCSV($"SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}';");
        }



        [KernelFunction, Description("Run SQL against the SunuElection database")]
        public string RunQuery(
            [Description("The query to run on SQL Server. When referencing tables, make sure to add the schema names.")] string query
        )
        {
            Console.WriteLine($"Running query...");
            return QueryAsCSV(query);
        }

        private string QueryAsCSV(string query)
        {
            var output = "[DATABASE RESULTS] \n";
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                try
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        output += reader.GetName(i);
                        if (i < reader.FieldCount - 1)
                            output += ",";
                    }
                    output += "\n";
                    while (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var columnName = reader.GetName(i);
                            output += reader[columnName].ToString();
                            if (i < reader.FieldCount - 1)
                                output += ",";
                        }
                        output += "\n";
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                finally
                {
                    reader.Close();
                }
            }
            return output;
        }
    }
}
