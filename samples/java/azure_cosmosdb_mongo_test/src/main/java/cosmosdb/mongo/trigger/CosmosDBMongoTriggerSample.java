package cosmosdb.mongo.trigger;

import com.microsoft.azure.functions.annotation.FunctionName;
import com.microsoft.azure.functions.cosmosdb.*;

import com.microsoft.azure.functions.annotation.*;
import com.microsoft.azure.functions.*;
import org.bson.Document;

import java.util.*;
import java.util.logging.Logger;

public class CosmosDBMongoTriggerSample {
    @FunctionName("OutputBindingSample")
    public void outputBindingRun(
            @TimerTrigger(name = "timer", schedule = "*/5 * * * * *") String timerInfo,
            @CosmosDBMongo(
                    databaseName = "%vCoreDatabaseBinding%",
                    collectionName = "%testcolbinding%",
                    connectionStringSetting = "%CosmosDBMongo%")
            OutputBinding<List<Document>> cosmosDBOutput,
            final ExecutionContext context
    ) {
        Logger log = context.getLogger();
        log.info("OutputBindingSample triggered");

        TestClass item = new TestClass();
        item.id = UUID.randomUUID().toString();
        item.someData = "some random data";

        Document doc = new Document("id", item.id).append("someData", item.someData);

        List<Document> documents = new ArrayList<>();
        documents.add(doc);
        cosmosDBOutput.setValue(documents);

        log.info("Inserted doc: " + doc.toJson());
    }

    public static class TestClass {
        public String id;
        public String someData;
    }
}
