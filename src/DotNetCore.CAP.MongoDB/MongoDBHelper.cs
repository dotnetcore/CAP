using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DotNetCore.CAP.MongoDB
{
    public class MongoDBHelper
    {
        FindOneAndUpdateOptions<BsonDocument> _options = new FindOneAndUpdateOptions<BsonDocument>()
        {
            ReturnDocument = ReturnDocument.After
        };
        public async Task<int> GetNextSequenceValueAsync(IMongoDatabase database, string collectionName)
        {
            //https://www.tutorialspoint.com/mongodb/mongodb_autoincrement_sequence.htm
            var collection = database.GetCollection<BsonDocument>("Counter");

            var updateDef = Builders<BsonDocument>.Update.Inc("sequence_value", 1);
            var result = await
            collection.FindOneAndUpdateAsync(new BsonDocument { { "_id", collectionName } }, updateDef, _options);
            if (result.TryGetValue("sequence_value", out var value))
            {
                return value.ToInt32();
            }
            throw new Exception("Unable to get next sequence value.");
        }

        public int GetNextSequenceValue(IMongoDatabase database, string collectionName)
        {
            var collection = database.GetCollection<BsonDocument>("Counter");

            var updateDef = Builders<BsonDocument>.Update.Inc("sequence_value", 1);
            var result = collection.FindOneAndUpdate(new BsonDocument { { "_id", collectionName } }, updateDef, _options);
            if (result.TryGetValue("sequence_value", out var value))
            {
                return value.ToInt32();
            }
            throw new Exception("Unable to get next sequence value.");
        }
    }
}