package com.microsoft.azure.functions.cosmosdb.trigger;

import com.microsoft.azure.functions.annotation.CustomBinding;
import java.lang.annotation.*;

@Target(ElementType.PARAMETER)
@Retention(RetentionPolicy.RUNTIME)
@CustomBinding(name = "cosmosDBMongoTrigger", type = "cosmosDBMongoTrigger", direction = "out")
public @interface CosmosDBMongoTrigger {
    String databaseName() default "";

    String collectionName() default "";

    boolean createIfNotExists() default false;

    String connectionStringSetting();

    MonitorLevel monitorLevel() default MonitorLevel.Collection;
}
