package com.microsoft.azure.functions.cosmosdb;

import com.microsoft.azure.functions.annotation.CustomBinding;
import java.lang.annotation.*;

@Target({ElementType.PARAMETER, ElementType.METHOD})
@Retention(RetentionPolicy.RUNTIME)
@CustomBinding(name = "cosmosDBMongo", type = "cosmosDBMongo", direction = "out")
public @interface CosmosDBMongo {
    String databaseName() default "";

    String collectionName() default "";

    String connectionStringSetting();

    boolean createIfNotExists() default false;

    String queryString() default "";
}
