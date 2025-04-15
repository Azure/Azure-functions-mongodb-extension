package com.microsoft.azure.functions.cosmosdb.trigger;

import com.mongodb.client.MongoClient;

public class CosmosDBMongoTriggerContext {
    private MongoClient mongoClient;

    private String databaseName = "";
    private String collectionName = "";
    private boolean createIfNotExists = false;
    private String connectionStringSetting = "";
    private MonitorLevel monitorLevel = MonitorLevel.Collection;

    public CosmosDBMongoTriggerContext(CosmosDBMongoTrigger annotation) {
        if (annotation != null) {
            this.databaseName = safe(annotation.databaseName());
            this.collectionName = safe(annotation.collectionName());
            this.connectionStringSetting = annotation.connectionStringSetting();
            this.createIfNotExists = annotation.createIfNotExists();

            if (annotation.monitorLevel() != null) {
                this.monitorLevel = annotation.monitorLevel();
            } else {
                resolveMonitorLevel(); // 根据 db/collection 推断
            }
        }
    }

    public void resolveMonitorLevel() {
        if (isEmpty(databaseName)) {
            this.monitorLevel = MonitorLevel.Cluster;
        } else if (isEmpty(collectionName)) {
            this.monitorLevel = MonitorLevel.Database;
        } else {
            this.monitorLevel = MonitorLevel.Collection;
        }
    }

    private String safe(String value) {
        return value != null ? value.trim() : "";
    }

    private boolean isEmpty(String value) {
        return value == null || value.trim().isEmpty();
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
        resolveMonitorLevel();
    }

    public String getCollectionName() {
        return collectionName;
    }

    public void setCollectionName(String collectionName) {
        this.collectionName = safe(collectionName);
        resolveMonitorLevel();
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

    public MonitorLevel getMonitorLevel() {
        return monitorLevel;
    }

    public void setMonitorLevel(MonitorLevel monitorLevel) {
        this.monitorLevel = monitorLevel;
    }
}
