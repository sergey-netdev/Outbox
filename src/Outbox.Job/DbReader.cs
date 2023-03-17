using MassTransit.SagaStateMachine;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outbox.Job
{
    internal class DbReader
    {
        private const string ConnString = "Server=localhost,14330;Initial Catalog=Putbox;User Id=SA;Password=G0neFishing;";
        private const int BatchSize = 10;

        public static async Task Read()
        {
            using (SqlConnection connection = new SqlConnection(ConnString))
            {
                await connection.OpenAsync();
                using (SqlCommand command = new SqlCommand(SQL.SelectForProcessing, connection))
                {
                    command.Parameters.AddWithValue("batchSize", BatchSize);

                    // The reader needs to be executed with the SequentialAccess behavior to enable network streaming
                    // Otherwise ReadAsync will buffer the entire BLOB into memory which can cause scalability issues or even OutOfMemoryExceptions
                    using (SqlDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess))
                    {
                        if (await reader.ReadAsync())
                        {
                            if (!(await reader.IsDBNullAsync(0)))
                            {
                                using (FileStream file = new FileStream("binarydata.bin", FileMode.Create, FileAccess.Write))
                                {
                                    using (Stream data = reader.GetStream(0))
                                    {
                                        // Asynchronously copy the stream from the server to the file we just created
                                        await data.CopyToAsync(file);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
