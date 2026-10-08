-- ============================================================================
-- Uppa Rental Management System - Canonical Database Schema
-- Target: MariaDB 10.4+ / MySQL (XAMPP default: root, no password)
-- Run this in phpMyAdmin / mysql client BEFORE using landlord/tenant pages.
-- ============================================================================

CREATE DATABASE IF NOT EXISTS rental_db
  CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE rental_db;

-- ----------------------------------------------------------------------------
-- Users (landlords + tenants). Canonical column names used across the app.
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS users (
	user_id       INT AUTO_INCREMENT PRIMARY KEY,
	username      VARCHAR(50)  NOT NULL UNIQUE,
	password      VARCHAR(255) NOT NULL,
	role          VARCHAR(20)  NOT NULL DEFAULT 'Tenant',   -- Landlord / Tenant
	first_name    VARCHAR(100) NOT NULL DEFAULT '',
	last_name     VARCHAR(100) NOT NULL DEFAULT '',
	email_address VARCHAR(150) NOT NULL DEFAULT '',
	gender        VARCHAR(20)  NOT NULL DEFAULT '',
	age           VARCHAR(10)  NOT NULL DEFAULT '',
	phone         VARCHAR(30)  NOT NULL DEFAULT '',
	address       VARCHAR(255) NOT NULL DEFAULT '',
	created_at    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB;

-- ----------------------------------------------------------------------------
-- Properties (units) owned/managed by a landlord.
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS properties (
	property_id            INT AUTO_INCREMENT PRIMARY KEY,
	landlord_id            INT NULL,
	name                   VARCHAR(150) NOT NULL,
	location               VARCHAR(255) NOT NULL DEFAULT '',
	number_of_rooms        INT NOT NULL DEFAULT 0,
	number_of_floors       INT NOT NULL DEFAULT 1,
	number_of_bathrooms    INT NOT NULL DEFAULT 0,
	maximum_capacity       INT NOT NULL DEFAULT 1,
	size_unit              INT NOT NULL DEFAULT 0,          -- sqft
	security_deposit       INT NOT NULL DEFAULT 0,
	monthly_rent           INT NOT NULL DEFAULT 0,
	property_type          VARCHAR(50) NOT NULL DEFAULT 'Studio',
	status                 VARCHAR(20) NOT NULL DEFAULT 'Available', -- Available/Reserved/Occupied/Draft
	amenities              VARCHAR(500) NOT NULL DEFAULT '', -- comma-separated enum names
	description            TEXT NULL,
	notes                  TEXT NULL,
	photo_paths            TEXT NULL,                        -- newline-delimited paths
	is_electric_included   TINYINT(1) NOT NULL DEFAULT 0,
	electric_bill          INT NOT NULL DEFAULT 0,
	electric_kwh_rate      DOUBLE NOT NULL DEFAULT 12.00,
	is_water_included      TINYINT(1) NOT NULL DEFAULT 0,
	water_bill             INT NOT NULL DEFAULT 0,
	water_rate_per_cbm     DOUBLE NOT NULL DEFAULT 50.00,
	is_wifi_included       TINYINT(1) NOT NULL DEFAULT 0,
	wifi_bill              INT NOT NULL DEFAULT 0,
	inquiry_count          INT NOT NULL DEFAULT 0,
	created_at             DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT fk_properties_landlord FOREIGN KEY (landlord_id)
		REFERENCES users(user_id) ON DELETE SET NULL
) ENGINE=InnoDB;

-- ----------------------------------------------------------------------------
-- Renters / leases. One row = an active/past lease OR a pending reservation.
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS renters (
	renter_id         INT AUTO_INCREMENT PRIMARY KEY,
	user_id           INT NULL,                              -- links to users if the renter has an account
	property_id       INT NULL,
	name              VARCHAR(150) NOT NULL,
	unit              VARCHAR(100) NOT NULL DEFAULT '',      -- display unit/property name
	contact           VARCHAR(50)  NOT NULL DEFAULT '',
	email             VARCHAR(150) NOT NULL DEFAULT '',
	address           VARCHAR(255) NOT NULL DEFAULT '',
	lease_start       DATE NULL,
	lease_end         DATE NULL,
	rental_term       VARCHAR(20) NOT NULL DEFAULT 'Long-Term',  -- Long-Term / Short-Term
	rate              DECIMAL(12,2) NOT NULL DEFAULT 0,      -- per month (long) / per day (short)
	months            INT NOT NULL DEFAULT 0,
	days              INT NOT NULL DEFAULT 0,
	status            VARCHAR(20) NOT NULL DEFAULT 'Active', -- Active / Pending / Past
	is_reservation    TINYINT(1) NOT NULL DEFAULT 0,
	reservation_paid  TINYINT(1) NOT NULL DEFAULT 0,
	hold_until        DATE NULL,
	advance_amount    DECIMAL(12,2) NOT NULL DEFAULT 0,
	deposit_amount    DECIMAL(12,2) NOT NULL DEFAULT 0,
	is_archived       TINYINT(1) NOT NULL DEFAULT 0,
	move_out_date     DATE NULL,
	archive_reason    VARCHAR(255) NOT NULL DEFAULT '',
	created_at        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT fk_renters_user FOREIGN KEY (user_id)
		REFERENCES users(user_id) ON DELETE SET NULL,
	CONSTRAINT fk_renters_property FOREIGN KEY (property_id)
		REFERENCES properties(property_id) ON DELETE SET NULL
) ENGINE=InnoDB;

-- ----------------------------------------------------------------------------
-- Invoices (billing). Canonical shape used by TenantBillingPage + BillingPage.
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS invoices (
	invoice_id   INT AUTO_INCREMENT PRIMARY KEY,
	user_id      INT NULL,                                   -- tenant being billed
	invoice_no   VARCHAR(30) NOT NULL UNIQUE,                -- e.g. INV-2001
	renter       VARCHAR(150) NOT NULL DEFAULT '',
	unit         VARCHAR(100) NOT NULL DEFAULT '',
	period       VARCHAR(30)  NOT NULL DEFAULT '',           -- e.g. "Oct 2026"
	due_date     DATE NULL,
	amount       DECIMAL(12,2) NOT NULL DEFAULT 0,
	status       VARCHAR(20) NOT NULL DEFAULT 'Pending',     -- Pending/Paid/Overdue
	created_at   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT fk_invoices_user FOREIGN KEY (user_id)
		REFERENCES users(user_id) ON DELETE SET NULL
) ENGINE=InnoDB;

-- ----------------------------------------------------------------------------
-- Payments (receipts). Canonical short column names used by TenantBillingPage.
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS payments (
	payment_id   INT AUTO_INCREMENT PRIMARY KEY,
	receipt_no   VARCHAR(30) NOT NULL DEFAULT '',
	invoice_id   INT NULL,
	user_id      INT NULL,
	period       VARCHAR(30) NOT NULL DEFAULT '',
	date_paid    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	method       VARCHAR(50) NOT NULL DEFAULT 'Cash',
	reference    VARCHAR(100) NOT NULL DEFAULT '',
	amount       DECIMAL(12,2) NOT NULL DEFAULT 0,
	created_at   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT fk_payments_invoice FOREIGN KEY (invoice_id)
		REFERENCES invoices(invoice_id) ON DELETE SET NULL,
	CONSTRAINT fk_payments_user FOREIGN KEY (user_id)
		REFERENCES users(user_id) ON DELETE SET NULL
) ENGINE=InnoDB;

-- ----------------------------------------------------------------------------
-- Reservations (bookings).
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS reservations (
	reservation_id   INT AUTO_INCREMENT PRIMARY KEY,
	user_id          INT NULL,
	renter_id        INT NULL,
	property_id      INT NULL,
	reservation_date DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	start_date       DATE NULL,
	end_date         DATE NULL,
	rental_duration  INT NOT NULL DEFAULT 0,                 -- months
	monthly_rate     DECIMAL(12,2) NOT NULL DEFAULT 0,
	total_rent       DECIMAL(12,2) NOT NULL DEFAULT 0,
	down_payment     DECIMAL(12,2) NOT NULL DEFAULT 0,
	status           VARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending/Active/Completed/Cancelled
	created_at       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT fk_res_user FOREIGN KEY (user_id)
		REFERENCES users(user_id) ON DELETE SET NULL,
	CONSTRAINT fk_res_property FOREIGN KEY (property_id)
		REFERENCES properties(property_id) ON DELETE SET NULL
) ENGINE=InnoDB;

-- ----------------------------------------------------------------------------
-- Bills (utility / misc charges) - used by tenant dashboard "pending amount".
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS bills (
	bill_id      INT AUTO_INCREMENT PRIMARY KEY,
	user_id      INT NULL,
	description  VARCHAR(255) NOT NULL DEFAULT '',
	amount       DECIMAL(12,2) NOT NULL DEFAULT 0,
	status       VARCHAR(20) NOT NULL DEFAULT 'Pending',     -- Pending/Paid
	due_date     DATE NULL,
	created_at   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT fk_bills_user FOREIGN KEY (user_id)
		REFERENCES users(user_id) ON DELETE SET NULL
) ENGINE=InnoDB;
