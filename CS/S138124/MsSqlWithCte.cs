using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using DevExpress.Xpo;
using DevExpress.Xpo.Helpers;
using DevExpress.Xpo.DB;
using DevExpress.Xpo.DB.Helpers;

namespace DevExpress.Xpo.DB.Cte {
    public class MSSqlConnectionProviderWithCte: MSSqlConnectionProvider {
        #region infrastructure
        public MSSqlConnectionProviderWithCte(IDbConnection connection, AutoCreateOption autoCreateOption)
            : base(connection, autoCreateOption) { }
        public new static IDataStore CreateProviderFromString(string connectionString, AutoCreateOption autoCreateOption, out IDisposable[] objectsToDisposeOnDisconnect) {
            IDbConnection connection = new SqlConnection(connectionString);
            objectsToDisposeOnDisconnect = new IDisposable[] { connection };
            return CreateProviderFromConnection(connection, autoCreateOption);
        }
        public new static IDataStore CreateProviderFromConnection(IDbConnection connection, AutoCreateOption autoCreateOption) {
            return new MSSqlConnectionProviderWithCte(connection, autoCreateOption);
        }
        static MSSqlConnectionProviderWithCte() {
            RegisterDataStoreProvider(XpoProviderTypeString, new DataStoreCreationFromStringDelegate(CreateProviderFromString));
            //RegisterDataStoreProvider("System.Data.SqlClient.SqlConnection", new DataStoreCreationFromConnectionDelegate(CreateProviderFromConnection));
            RegisterFactory(new MSSqlWithCteProviderFactory());
        }
        public new static void Register() { }
        public new const string XpoProviderTypeString = "MSSqlServerWithCte";
        public new static string GetConnectionString(string server, string userid, string password, string database) {
            return GetConnectionStringForType(XpoProviderTypeString, server, userid, password, database);
        }
        public new static string GetConnectionString(string server, string database) {
            return GetConnectionStringForType(XpoProviderTypeString, server, database);
        }
        public new static string GetConnectionStringWithAttach(string server, string userid, string password, string attachDbFilename, bool userInstance) {
            return GetConnectionStringForTypeWithAttach(XpoProviderTypeString, server, userid, password, attachDbFilename, userInstance);
        }
        public new static string GetConnectionStringWithAttach(string server, string attachDbFilename, bool userInstance) {
            return GetConnectionStringForTypeWithAttach(XpoProviderTypeString, server, attachDbFilename, userInstance);
        }
        #endregion

        readonly Dictionary<string, string> registeredCte = new Dictionary<string, string>();
        public void RegisterCte(string cteName, string cteBody) {
            lock(SyncRoot) {
                registeredCte[cteName] = cteBody;
            }
        }
        public void UnregisterCte(string cteName) {
            lock(SyncRoot) {
                registeredCte.Remove(cteName);
            }
        }
        Dictionary<string, string> usedCtes;
        string ProcessPossibleCte(string maybeCteName) {
            string cteBody = null;
            if(!registeredCte.TryGetValue(maybeCteName, out cteBody))
                return null;
            if(usedCtes == null)
                usedCtes = new Dictionary<string, string>();
            usedCtes[maybeCteName] = cteBody;
            return maybeCteName;
        }
        public override string FormatTable(string schema, string tableName) {
            return ProcessPossibleCte(tableName) ?? base.FormatTable(schema, tableName);
        }
        public override string FormatTable(string schema, string tableName, string tableAlias) {
            string possibleCte = ProcessPossibleCte(tableName);
            if(possibleCte != null)
                return possibleCte + ' ' + tableAlias;
            else
                return base.FormatTable(schema, tableName, tableAlias);
        }
        public override string FormatSelect(string selectedPropertiesSql, string fromSql, string whereSql, string orderBySql, string groupBySql, string havingSql, int skipSelectedRecords, int topSelectedRecords) {
            string selectBody = base.FormatSelect(selectedPropertiesSql, fromSql, whereSql, orderBySql, groupBySql, havingSql, skipSelectedRecords, topSelectedRecords);
            if(usedCtes == null || usedCtes.Count == 0)
                return selectBody;
            List<string> ctes = new List<string>(usedCtes.Count);
            foreach(KeyValuePair<string, string> cte in usedCtes) {
                ctes.Add(cte.Key + ' ' + cte.Value);
            }
            usedCtes = null;
            return string.Format("with\n{0}\n{1}", string.Join(",\n", ctes.ToArray()), selectBody);
        }
        protected override object DoInternal(string command, object args) {
            switch(command) {
                case CteExtensions.CommandRegisterCteName: {
                        if(args == null)
                            throw new ArgumentNullException("args");
                        string cteExpression = args.ToString();
                        int spacePos = cteExpression.IndexOf(' ');
                        if(spacePos < 1)
                            throw new ArgumentException("malformed args");
                        string cteName = cteExpression.Substring(0, spacePos);
                        string cteBody = cteExpression.Substring(spacePos + 1);
                        this.RegisterCte(cteName, cteBody);
                        return null;
                    }
                case CteExtensions.CommandUnregisterCteName: {
                        if(args == null)
                            throw new ArgumentNullException("args");
                        this.UnregisterCte(args.ToString());
                        return null;
                    }
            }
            return base.DoInternal(command, args);
        }
        protected override UpdateSchemaResult ProcessUpdateSchema(bool skipIfFirstTableNotExists, params DBTable[] tables) {
            List<DBTable> realTables = new List<DBTable>(tables.Length);
            foreach(DBTable t in tables) {
                if(t.Name.StartsWith("CteStructure_")) {
                    if(realTables.Count == 0)
                        skipIfFirstTableNotExists = false;
                } else {
                    realTables.Add(t);
                }
            }
            if(realTables.Count == 0)
                return UpdateSchemaResult.SchemaExists;
            return base.ProcessUpdateSchema(skipIfFirstTableNotExists, realTables.ToArray());
        }
    }
    public static class CteExtensions {
        public const string CommandRegisterCteName = "RegisterCte";
        public const string CommandUnregisterCteName = "UnregisterCte";
        public static void RegisterCte(this ICommandChannel channel, string cteName, string cteBody) {
            channel.Do(CommandRegisterCteName, cteName + ' ' + cteBody);
        }
        public static void UnregisterCte(this ICommandChannel channel, string cteName) {
            channel.Do(CommandUnregisterCteName, cteName);
        }
    }
    #region infastructure
    public class MSSqlWithCteProviderFactory: MSSqlProviderFactory {
        public override IDataStore CreateProviderFromConnection(IDbConnection connection, AutoCreateOption autoCreateOption) {
            return MSSqlConnectionProviderWithCte.CreateProviderFromConnection(connection, autoCreateOption);
        }
        public override IDataStore CreateProviderFromString(string connectionString, AutoCreateOption autoCreateOption, out IDisposable[] objectsToDisposeOnDisconnect) {
            return MSSqlConnectionProviderWithCte.CreateProviderFromString(connectionString, autoCreateOption, out objectsToDisposeOnDisconnect);
        }
        public override string GetConnectionString(Dictionary<string, string> parameters) {
            string connectionString;
            bool useIntegratedSecurity = false;
            if(parameters.ContainsKey(UseIntegratedSecurityParamID)) {
                useIntegratedSecurity = Convert.ToBoolean(parameters[UseIntegratedSecurityParamID]);
            }
            if(useIntegratedSecurity) {
                if(!parameters.ContainsKey(ServerParamID) || !parameters.ContainsKey(DatabaseParamID)) { return null; }
                connectionString = MSSqlConnectionProviderWithCte.GetConnectionString(parameters[ServerParamID], parameters[DatabaseParamID]);
            } else {
                if(!parameters.ContainsKey(ServerParamID) || !parameters.ContainsKey(DatabaseParamID) ||
                    !parameters.ContainsKey(UserIDParamID) || !parameters.ContainsKey(PasswordParamID)) {
                    return null;
                }
                connectionString = MSSqlConnectionProviderWithCte.GetConnectionString(parameters[ServerParamID], parameters[UserIDParamID], parameters[PasswordParamID], parameters[DatabaseParamID]);
            }
            return connectionString;
        }
        public override string ProviderKey { get { return MSSqlConnectionProviderWithCte.XpoProviderTypeString; } }
    }
    #endregion infastructure
}