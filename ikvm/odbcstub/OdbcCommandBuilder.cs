﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace System.Data.Odbc
{
    public sealed class OdbcCommandBuilder : DbCommandBuilder
    {
        public OdbcCommandBuilder()
        {
            throw new NotImplementedException();
        }

        public OdbcCommandBuilder(OdbcDataAdapter da)
        {
            throw new NotImplementedException();
        }

        protected override void ApplyParameterInfo(DbParameter parameter, DataRow row, StatementType statementType, bool whereClause)
        {
            throw new NotImplementedException();
        }

        protected override string GetParameterName(int parameterOrdinal)
        {
            throw new NotImplementedException();
        }

        protected override string GetParameterName(string parameterName)
        {
            throw new NotImplementedException();
        }

        protected override string GetParameterPlaceholder(int parameterOrdinal)
        {
            throw new NotImplementedException();
        }

        protected override void SetRowUpdatingHandler(DbDataAdapter adapter)
        {
            throw new NotImplementedException();
        }
    }
}
