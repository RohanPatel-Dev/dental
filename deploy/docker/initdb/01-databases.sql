-- One logical database per host for jobs, and one for the extracted module.
-- Hangfire owns its schema outright; two hosts sharing one would fight over the same queues.
CREATE DATABASE dental_hangfire_api;
CREATE DATABASE dental_hangfire_notifications;
CREATE DATABASE dental_notifications;
