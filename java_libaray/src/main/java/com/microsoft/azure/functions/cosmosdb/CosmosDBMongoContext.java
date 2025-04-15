package com.microsoft.azure.functions.cosmosdb;

import com.mongodb.client.MongoClient;

public class CosmosDBMongoContext {
    private MongoClient mongoClient;
    private String databaseName = "";
    private String collectionName = "";
    private boolean createIfNotExists = false;
    private String connectionStringSetting = "";
    private String queryString = "";

    public CosmosDBMongoContext() {
    }

    public CosmosDBMongoContext(CosmosDBMongo annotation) {
        if (annotation != null) {
            this.databaseName = safe(annotation.databaseName());
            this.collectionName = safe(annotation.collectionName());
            this.createIfNotExists = annotation.createIfNotExists();
            this.connectionStringSetting = annotation.connectionStringSetting();
            this.queryString = annotation.queryString();
        }
    }

    private String safe(String value) {
        return value != null ? value.trim() : "";
    }

    public MongoClient getMongoClient() {
        return mongoClient;
    }

    public void setMongoClient(MongoClient mongoClient) {
        this.mongoClient = mongoClient;
    }

    public String getDatabaseName() {
        return databaseName;
    }

    public void setDatabaseName(String databaseName) {
        this.databaseName = safe(databaseName);
    }

    public String getCollectionName() {
        return collectionName;
    }

    public void setCollectionName(String collectionName) {
        this.collectionName = safe(collectionName);
    }

    public boolean isCreateIfNotExists() {
        return createIfNotExists;
    }

    public void setCreateIfNotExists(boolean createIfNotExists) {
        this.createIfNotExists = createIfNotExists;
    }

    public String getConnectionStringSetting() {
        return connectionStringSetting;
    }

    public void setConnectionStringSetting(String connectionStringSetting) {
        this.connectionStringSetting = connectionStringSetting;
    }

    public String getQueryString() {
        return queryString;
    }

    public void setQueryString(String queryString) {
        this.queryString = queryString;
    }
}
