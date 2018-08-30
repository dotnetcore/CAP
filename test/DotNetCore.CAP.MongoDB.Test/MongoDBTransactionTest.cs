using System;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace DotNetCore.CAP.MongoDB.Test
{
    [Collection("MongoDB")]
    public class MongoDBTransactionTest : DatabaseTestHost
    {
        [Fact]
        public void MongoDB_Connection_Test()
        {
            var names = MongoClient.ListDatabaseNames();
            names.ToList().Should().NotBeNullOrEmpty();
        }

        [Fact(Skip = "Because of Appveyor dose not support MongoDB 4.0, so we skip this test for now.")]
        public void Transaction_Test()
        {
            var document = new BsonDocument
                {
                    { "name", "MongoDB" },
                    { "type", "Database" },
                    { "count", 1 },
                    { "info", new BsonDocument
                        {
                            { "x", 203 },
                            { "y", 102 }
                        }}
                };
            var db = MongoClient.GetDatabase("test");
            var collection1 = db.GetCollection<BsonDocument>("test1");
            var collection2 = db.GetCollection<BsonDocument>("test2");
            using (var sesstion = MongoClient.StartSession())
            {
                sesstion.StartTransaction();
                collection1.InsertOne(document);
                collection2.InsertOne(document);
                sesstion.CommitTransaction();
            }
            var filter = new BsonDocument("name", "MongoDB");
            collection1.CountDocuments(filter).Should().BeGreaterThan(0);
            collection2.CountDocuments(filter).Should().BeGreaterThan(0);
        }

        [Fact(Skip = "Because of Appveyor dose not support MongoDB 4.0, so we skip this test for now.")]
        public void Transaction_Rollback_Test()
        {
            var document = new BsonDocument
            {
                {"name", "MongoDB"},
                {"date", DateTimeOffset.Now.ToString()}
            };
            var db = MongoClient.GetDatabase("test");

            var collection = db.GetCollection<BsonDocument>("test3");
            var collection4 = db.GetCollection<BsonDocument>("test4");

            using (var session = MongoClient.StartSession())
            {
                session.IsInTransaction.Should().BeFalse();
                session.StartTransaction();
                session.IsInTransaction.Should().BeTrue();
                collection.InsertOne(session, document);
                collection4.InsertOne(session, new BsonDocument { { "name", "MongoDB" } });

                session.AbortTransaction();
            }
            var filter = new BsonDocument("name", "MongoDB");
            collection.CountDocuments(filter).Should().Be(0);
            collection4.CountDocuments(filter).Should().Be(0);
        }
    }
}
