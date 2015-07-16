﻿using System;
using TDSM.API.Data;
using MySql.Data.MySqlClient;
using System.Data;
using System.Collections.Generic;

namespace TDSM.Data.MySQL
{
    public class MySQLConnector : IDataConnector
    {
        private MySqlConnection _connection;

        public QueryBuilder GetBuilder(string pluginName)
        {
            return new MySQLQueryBuilder(pluginName);
        }

        public QueryBuilder GetBuilder(string pluginName, string command, System.Data.CommandType type)
        {
            return new MySQLQueryBuilder(pluginName, command, type);
        }

        public MySQLConnector(string connectionString)
        {
            _connection = new MySqlConnection();
            _connection.ConnectionString = connectionString;
        }

        public void Open()
        {
            _connection.Open();
        }

        bool IDataConnector.Execute(QueryBuilder builder)
        {
            return false;
        }

        T IDataConnector.ExecuteScalar<T>(QueryBuilder builder)
        {
            return default(T);
        }

        DataSet IDataConnector.ExecuteDataSet(QueryBuilder builder)
        {
            return null;
        }

        T[] IDataConnector.ExecuteArray<T>(QueryBuilder builder)
        {
            return null;
        }

        public override string ToString()
        {
            return "[MySQLConnector]";
        }
    }

    public class MySQLQueryBuilder : QueryBuilder
    {
        private List<MySql.Data.MySqlClient.MySqlParameter> _params;

        public MySQLQueryBuilder(string pluginName)
            : base(pluginName)
        {
            _params = new  List<MySql.Data.MySqlClient.MySqlParameter>();
        }

        public MySQLQueryBuilder(string pluginName, string command, System.Data.CommandType type)
            : base(pluginName, command, type)
        {
            _params = new  List<MySql.Data.MySqlClient.MySqlParameter>();
        }


        public override QueryBuilder AddParam(string name, object value)
        {
            var paramKey = "prm" + name;
            _params.Add(new MySqlParameter(paramKey, value));
            return this;
        }

        public override QueryBuilder TableExists(string name)
        {
            Append("SHOW TABLES LIKE '{0}'", base.GetTableName(name));
            return this;
        }

        public override QueryBuilder TableCreate(string name, params TableColumn[] columns)
        {
            Append("CREATE TABLE '{0}' ", base.GetTableName(name));

            if (columns != null && columns.Length > 0)
            {
                for (var x = 0; x < columns.Length; x++)
                {
                    var col = columns[x];

                    if (col.DataType is Int16)
                    {

                    }
                    else if (col.DataType is Int32)
                    {

                    }
                    else if (col.DataType is Int64)
                    {

                    }
                    else if (col.DataType is String)
                    {
                        var isVarChar = col.MinScale.HasValue && col.MaxScale.HasValue;
                    }
                    else
                    {
                        throw new NotSupportedException(String.Format("Data type for column '{0}' is not supported", col.Name));
                    }

                    if (x + 1 < columns.Length)
                        Append(",");
                }
            }

            return this;
        }

        public  override QueryBuilder TableDrop(string name)
        {
            Append("DROP TABLE IF EXISTS {0}", base.GetTableName(name));
            return this;
        }

        public override QueryBuilder ProcedureExists(string name)
        {
            return this;
        }

        public override QueryBuilder ProcedureCreate(string name, string contents, params DataParameter[] parameters)
        {
            return this;
        }

        public override QueryBuilder ProcedureDrop(string name)
        {
            return this;
        }


        public override QueryBuilder Select(params string[] expression)
        {
            Append("SELECT ");

            if (expression != null && expression.Length > 0)
            {
                Append(String.Join(",", expression));
            }
            return this;
        }

        public override QueryBuilder All()
        {
            Append("* ");
            return this;
        }

        public override QueryBuilder From(string tableName)
        {
            Append("FROM ");
            Append(base.GetTableName(tableName));
            Append(" ");
            return this;
        }

        public override QueryBuilder Where(params WhereFilter[] clause)
        {
            Append("WHERE ");

            if (clause != null && clause.Length > 0)
            {
                for (var x = 0; x < clause.Length; x++)
                {
                    if (x > 0)
                        Append("AND ");

                    var xp = clause[x];

                    Append(xp.Column);

                    switch (xp.Expression)
                    {
                        case WhereExpression.EqualTo:
                            Append(" = ");
                            break;
                        case WhereExpression.NotEqualTo:
                            Append(" = ");
                            break;
                        case WhereExpression.Like:
                            Append(" LIKE ");
                            break;
                    }

                    var paramKey = "prm" + xp.Column;
                    _params.Add(new MySqlParameter(paramKey, xp.Value));
                    Append(paramKey);
                    Append(" ");
                }
            }

            return this;
        }

        public override QueryBuilder Count(string expression = null)
        {
            Append("COUNT (");
            Append(expression ?? "*");
            Append(") ");
            return this;
        }

        public override QueryBuilder Delete()
        {
            Append("DELETE ");
            return this;
        }

        public override QueryBuilder InsertInto(string tableName, params DataParameter[] values)
        {
            Append("INSERT INTO ");
            Append(base.GetTableName(tableName));

            if (values != null && values.Length > 0)
            {
                //Columns
                Append(" ( ");
                for (var x = 0; x < values.Length; x++)
                {
                    Append(values[x].Name);

                    if (x + 1 < values.Length)
                        Append(",");
                }
                Append(" ) ");

                //Values
                Append(" VALUES ( ");
                for (var x = 0; x < values.Length; x++)
                {
                    var prm = values[x];
                    var paramKey = "prm" + prm.Name;

                    Append(paramKey);
                    if (x + 1 < values.Length)
                        Append(",");

                    _params.Add(new MySqlParameter(paramKey, prm.Value));
                    Append(paramKey);
                }
                Append(" ) ");
            }
            return this;
        }

        public override QueryBuilder Update(string tableName, DataParameter[] values)
        {
            Append("UPDATE ");
            Append(base.GetTableName(tableName));

            if (values != null && values.Length > 0)
            {
                Append(" SET ");

                for (var x = 0; x < values.Length; x++)
                {
                    var prm = values[x];
                    var paramKey = "prm" + prm.Name;

                    Append(prm.Name);
                    Append("=");
                    Append(paramKey);
                    Append(" ");


                    if (x + 1 < values.Length)
                        Append(",");

                    _params.Add(new MySqlParameter(paramKey, prm.Value));
                    Append(paramKey);
                }
            }

            return this;
        }

    }
}

