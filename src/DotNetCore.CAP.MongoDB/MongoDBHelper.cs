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
        public async Task<int> GetNextSequenceValueAsync(IMongoDatabase database, string collectionName, IClientSessionHandle session = null)
        {
            //https://www.tutorialspoint.com/mongodb/mongodb_autoincrement_sequence.htm
            var collection = database.GetCollection<BsonDocument>("Counter");

            var updateDef = Builders<BsonDocument>.Update.Inc("sequence_value", 1);
            var filter = new BsonDocument { { "_id", collectionName } };

            BsonDocument result;
            if (session == null)
            {
                result = await collection.FindOneAndUpdateAsync(filter, updateDef, _options);
            }
            else
            {
                result = await collection.FindOneAndUpdateAsync(session, filter, updateDef, _options);
            }

            if (result.TryGetValue("sequence_value", out var value))
            {
                return value.ToInt32();
            }
            throw new Exception("Unable to get next sequence value.");
        }

        public int GetNextSequenceValue(IMongoDatabase database, string collectionName, IClientSessionHandle session = null)
        {
            var collection = database.GetCollection<BsonDocument>("Counter");

            var filter = new BsonDocument { { "_id", collectionName } };
            var updateDef = Builders<BsonDocument>.Update.Inc("sequence_value", 1);

            BsonDocument result;
            if (session == null)
            {
                result = collection.FindOneAndUpdate(filter, updateDef, _options);
            }
            else
            {
                result = collection.FindOneAndUpdate(session, filter, updateDef, _options);
            }

            if (result.TryGetValue("sequence_value", out var value))
            {
                return value.ToInt32();
            }
            throw new Exception("Unable to get next sequence value.");
        }
    }
}