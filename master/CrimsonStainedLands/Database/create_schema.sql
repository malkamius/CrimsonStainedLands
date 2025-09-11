-- SQL script to create schema and table for CrimsonStainedLands Database class

-- Create the database schema if it doesn't exist
CREATE DATABASE IF NOT EXISTS csl CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE csl;

-- Create the 'helps' table
CREATE TABLE IF NOT EXISTS helps (
    vnum INT NOT NULL PRIMARY KEY,
    keywords VARCHAR(255) NOT NULL,
    text TEXT NOT NULL,
    level INT NOT NULL,
    last_updated DATETIME,
    last_updated_by VARCHAR(100),
    UNIQUE KEY uq_keywords (keywords)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
