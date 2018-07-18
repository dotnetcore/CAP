using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DotNetCore.CAP.MongoDB
{
    public class MongoDBHelper
    {
        public async Task<int> GetNextSequenceValueAsync<TCollection>(IMongoDatabase database)
        {
            //https://www.tutorialspoint.com/mongodb/mongodb_autoincrement_sequence.htm
            var collection = database.GetCollection<BsonDocument>("Counter");

            var updateDef = Builders<BsonDocument>.Update.Inc("sequence_value", 1);
            var result = await
            collection.FindOneAndUpdateAsync(new BsonDocument { { "_id", nameof(TCollection) } }, updateDef);
            if (result.TryGetValue("sequence_value", out var value))
            {
                return value.ToInt32();
            }
            throw new Exception("Unable to get next sequence value.");
        }

        public int GetNextSequenceValue<TCollection>(IMongoDatabase database)
        {
            var collection = database.GetCollection<BsonDocument>("Counter");

            var updateDef = Builders<BsonDocument>.Update.Inc("sequence_value", 1);
            var result = collection.FindOneAndUpdate(new BsonDocument { { "_id", nameof(TCollection) } }, updateDef);
            if (result.TryGetValue("sequence_value", out var value))
            {
                return value.ToInt32();
            }
            throw new Exception("Unable to get next sequence value.");
        }
    }
}