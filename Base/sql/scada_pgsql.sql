-- Database: scada

CREATE USER scada WITH PASSWORD 'jw8s0F4';

CREATE TABLESPACE scada_catalog OWNER scada LOCATION 'D:/postgresql/data/dbs'; -- Set your own absolute path here

DROP DATABASE IF EXISTS scada;

CREATE DATABASE scada
    WITH
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'Russian_Russia.1251'
    LC_CTYPE = 'Russian_Russia.1251'
    TABLESPACE = scada_catalog
    CONNECTION LIMIT = -1;

COMMENT ON DATABASE scada
    IS 'scada database for postgresql';

CREATE SCHEMA scada.scada_schema
    AUTHORIZATION scada;

COMMENT ON SCHEMA scada.scada_schema
    IS 'default schema for scada database';

-- Creates tables and indexes
CREATE TABLE IF NOT EXISTS scada_schema.CmdType
( 
	CmdTypeID int  NOT NULL,
	Name      varchar(50) UNIQUE NOT NULL,
	Descr     varchar(100) NULL,
	PRIMARY KEY (CmdTypeID)
) TABLESPACE scada_catalog;

COMMENT ON TABLE scada_schema.CmdType
    IS 'Command types of devices';

CREATE TABLE IF NOT EXISTS scada_schema.CmdVal
( 
	CmdValID  int  NOT NULL ,
	Name      varchar(50)  NOT NULL ,
	Val       varchar(100)  NOT NULL ,
	Descr     varchar(100)  NULL ,
	PRIMARY KEY (CmdValID)
) TABLESPACE scada_catalog;

COMMENT ON TABLE scada_schema.CmdVal
    IS 'Commands values';

CREATE TABLE IF NOT EXISTS scada_schema.CnlType
( 
	CnlTypeID            int  NOT NULL ,
	Name                 varchar(50)  NOT NULL ,
	ShtName              varchar(20)  NOT NULL ,
	Descr                varchar(100)  NULL ,
	PRIMARY KEY (CnlTypeID)
) TABLESPACE scada_catalog;

COMMENT ON TABLE scada_schema.CnlType
    IS 'Type definitions of channels';

CREATE TABLE IF NOT EXISTS scada_schema.CommLine
( 
	CommLineNum          int  NOT NULL ,
	Name                 varchar(50)  NOT NULL ,
	Descr                varchar(100)  NULL ,
	PRIMARY KEY (CommLineNum)
) TABLESPACE scada_catalog;

COMMENT ON TABLE scada_schema.CommLine
    IS 'Communication lines';

CREATE TABLE IF NOT EXISTS scada_schema.CtrlCnl
( 
	CtrlCnlNum           int  NOT NULL ,
	Active               bit  NOT NULL ,
	Name                 varchar(50)  NOT NULL ,
	CmdTypeID            int  NOT NULL ,
	ObjNum               int  NULL ,
	KPNum                int  NULL ,
	CmdNum               int  NULL ,
	CmdValID             int  NULL ,
	FormulaUsed          bit  NOT NULL ,
	Formula              varchar(100)  NULL ,
	EvEnabled            bit  NOT NULL ,
	ModifiedDT           TIMESTAMP WITH TIME ZONE NOT NULL ,
	PRIMARY KEY (CtrlCnlNum)
) TABLESPACE scada_catalog;

COMMENT ON TABLE scada_schema.CtrlCnl
    IS 'Control channels';

CREATE INDEX idx_CtrlCnl_KPNum ON scada_schema.CtrlCnl (KPNum);
CREATE INDEX idx_CtrlCnl_ObjNum ON scada_schema.CtrlCnl (ObjNum);

CREATE TABLE IF NOT EXISTS scada_schema.EvType
( 
	CnlStatus            int  NOT NULL ,
	Name                 varchar(50)  NOT NULL ,
	Color                varchar(20)  NULL ,
	Descr                varchar(100)  NULL ,
	PRIMARY KEY (CnlStatus)
) TABLESPACE scada_catalog;

COMMENT ON TABLE scada_schema.EvType
    IS 'Types of events';

CREATE TABLE IF NOT EXISTS scada_schema.Format
( 
	FormatID             int  NOT NULL ,
	Name                 varchar(50)  NOT NULL ,
	ShowNumber           bit  NOT NULL ,
	DecDigits            int  NULL ,
	PRIMARY KEY (FormatID)
) TABLESPACE scada_catalog;

COMMENT ON TABLE scada_schema.Format
    IS 'Value formats definitions';

CREATE TABLE IF NOT EXISTS scada_schema.Formula
( 
	Name                 varchar(50)  NOT NULL ,
	Source               varchar(1000)  NOT NULL ,
	Descr                varchar(100)  NULL ,
	PRIMARY KEY (Name)
) TABLESPACE scada_catalog;

COMMENT ON TABLE scada_schema.Formula
    IS 'Formulas definitions';

CREATE TABLE IF NOT EXISTS scada_schema.InCnl
( 
	CnlNum               int  NOT NULL ,
	Active               bit  NOT NULL ,
	Name                 varchar(50)  NOT NULL ,
	CnlTypeID            int  NOT NULL ,
	ObjNum               int  NULL ,
	KPNum                int  NULL ,
	Signal               int  NULL ,
	FormulaUsed          bit  NOT NULL ,
	Formula              varchar(100)  NULL ,
	Averaging            bit  NOT NULL ,
	ParamID              int  NULL ,
	FormatID             int  NULL ,
	UnitID               int  NULL ,
	CtrlCnlNum           int  NULL ,
	EvEnabled            bit  NOT NULL ,
	EvSound              bit  NOT NULL ,
	EvOnChange           bit  NOT NULL ,
	EvOnUndef            bit  NOT NULL ,
	LimLowCrash          float  NULL ,
	LimLow               float  NULL ,
	LimHigh              float  NULL ,
	LimHighCrash         float  NULL ,
	ModifiedDT           TIMESTAMP WITH TIME ZONE NOT NULL ,
	PRIMARY KEY (CnlNum)
) TABLESPACE scada_catalog;

COMMENT ON TABLE scada_schema.InCnl
    IS 'Input channels';

CREATE INDEX idx_InCnl_KPNum ON scada_schema.InCnl (KPNum);
CREATE INDEX idx_InCnl_ObjNum ON scada_schema.InCnl (ObjNum);

CREATE TABLE IF NOT EXISTS scada_schema.Interface
( 
	ItfID                int  NOT NULL ,
	Name                 varchar(50)  NOT NULL ,
	Descr                varchar(100)  NULL ,
	PRIMARY KEY (ItfID)
) TABLESPACE scada_catalog;

COMMENT ON TABLE scada_schema.Interface
    IS 'Interfaces';

CREATE TABLE IF NOT EXISTS scada_schema.KP
( 
	KPNum                int  NOT NULL ,
	Name                 varchar(50)  NOT NULL ,
	KPTypeID             int  NOT NULL ,
	Address              int  NULL ,
	CallNum              varchar(20)  NULL ,
	CommLineNum          int  NULL ,
	Descr                varchar(100)  NULL ,
	PRIMARY KEY (KPNum)
) TABLESPACE scada_catalog;

COMMENT ON TABLE scada_schema.KP
    IS 'KP - virtual devices difinitions';

CREATE TABLE IF NOT EXISTS scada_schema.KPType
( 
	KPTypeID             int  NOT NULL ,
	Name                 varchar(50)  NOT NULL ,
	DllFileName          varchar(20)  NULL ,
	Descr                varchar(100)  NULL ,
	PRIMARY KEY (KPTypeID)
) TABLESPACE scada_catalog;

COMMENT ON TABLE scada_schema.KPType
    IS 'Virtual devices types';

CREATE TABLE IF NOT EXISTS scada_schema.Obj
( 
	ObjNum               int  NOT NULL ,
	Name                 varchar(50)  NOT NULL ,
	Descr                varchar(100)  NULL ,
	PRIMARY KEY (ObjNum)
) TABLESPACE scada_catalog;

COMMENT ON TABLE scada_schema.Obj
    IS 'Objects definitions';

CREATE TABLE IF NOT EXISTS scada_schema.Param
( 
	ParamID              int  NOT NULL ,
	Name                 varchar(50)  NOT NULL ,
	Sign                 varchar(20)  NULL ,
	IconFileName         varchar(20)  NULL ,
	PRIMARY KEY (ParamID)
) TABLESPACE scada_catalog;

COMMENT ON TABLE scada_schema.Param
    IS 'Parameters of channels';

CREATE TABLE IF NOT EXISTS scada_schema.Right
( 
	ItfID                int  NOT NULL ,
	RoleID               int  NOT NULL ,
	ViewRight            bit  NOT NULL ,
	CtrlRight            bit  NOT NULL ,
	PRIMARY KEY (ItfID,RoleID)
) TABLESPACE scada_catalog;

COMMENT ON TABLE scada_schema.Right
    IS 'Users rights';

CREATE TABLE IF NOT EXISTS scada_schema.Role
( 
	RoleID               int  NOT NULL ,
	Name                 varchar(50)  NOT NULL ,
	Descr                varchar(100)  NULL ,
	PRIMARY KEY (RoleID)
) TABLESPACE scada_catalog;

COMMENT ON TABLE scada_schema.Role
    IS 'Users roles';

CREATE TABLE IF NOT EXISTS scada_schema.Unit
( 
	UnitID               int  NOT NULL ,
	Name                 varchar(50)  NOT NULL ,
	Sign                 varchar(100)  NOT NULL ,
	Descr                varchar(100)  NULL ,
	PRIMARY KEY (UnitID)
) TABLESPACE scada_catalog;

COMMENT ON TABLE scada_schema.Unit
    IS 'Measurement units definitions';

CREATE TABLE IF NOT EXISTS scada_schema.User
( 
	UserID               int  NOT NULL ,
	Name                 varchar(50)  NOT NULL ,
	Password             varchar(20)  NULL ,
	RoleID               int  NOT NULL ,
	Descr                varchar(100)  NULL ,
	PRIMARY KEY (UserID)
) TABLESPACE scada_catalog;

COMMENT ON TABLE scada_schema.User
    IS 'Users';

-- Configure references
ALTER TABLE scada_schema.CtrlCnl
	ADD CONSTRAINT fk_CtrlCnl_ObjNum FOREIGN KEY (ObjNum) REFERENCES scada_schema.Obj(ObjNum);
ALTER TABLE scada_schema.CtrlCnl
	ADD CONSTRAINT fk_CtrlCnl_KPNum FOREIGN KEY (KPNum) REFERENCES scada_schema.KP(KPNum);
ALTER TABLE scada_schema.CtrlCnl
	ADD CONSTRAINT fk_CtrlCnl_CmdTypeID FOREIGN KEY (CmdTypeID) REFERENCES scada_schema.CmdType(CmdTypeID);
ALTER TABLE scada_schema.CtrlCnl
	ADD CONSTRAINT fk_CtrlCnl_CmdValID FOREIGN KEY (CmdValID) REFERENCES scada_schema.CmdVal(CmdValID);
ALTER TABLE scada_schema.InCnl
	ADD CONSTRAINT fk_InCnl_KPNum FOREIGN KEY (KPNum) REFERENCES scada_schema.KP(KPNum);
ALTER TABLE scada_schema.InCnl
	ADD CONSTRAINT fk_InCnl_ObjNum FOREIGN KEY (ObjNum) REFERENCES scada_schema.Obj(ObjNum);
ALTER TABLE scada_schema.InCnl
	ADD CONSTRAINT fk_InCnl_CnlTypeID FOREIGN KEY (CnlTypeID) REFERENCES scada_schema.CnlType(CnlTypeID);
ALTER TABLE scada_schema.InCnl
	ADD CONSTRAINT fk_InCnl_UnitID FOREIGN KEY (UnitID) REFERENCES scada_schema.Unit(UnitID);
ALTER TABLE scada_schema.InCnl
	ADD CONSTRAINT fk_InCnl_FormatID FOREIGN KEY (FormatID) REFERENCES scada_schema.Format(FormatID);
ALTER TABLE scada_schema.InCnl
	ADD CONSTRAINT fk_InCnl_ParamID FOREIGN KEY (ParamID) REFERENCES scada_schema.Param(ParamID);
ALTER TABLE scada_schema.InCnl
	ADD CONSTRAINT fk_InCnl_CtrlCnlNum FOREIGN KEY (CtrlCnlNum) REFERENCES scada_schema.CtrlCnl(CtrlCnlNum);
ALTER TABLE scada_schema.KP
	ADD CONSTRAINT fk_KP_KPTypeID FOREIGN KEY (KPTypeID) REFERENCES scada_schema.KPType(KPTypeID);
ALTER TABLE scada_schema.KP
	ADD CONSTRAINT fk_KP_CommLineNum FOREIGN KEY (CommLineNum) REFERENCES scada_schema.CommLine(CommLineNum);
ALTER TABLE scada_schema.Right
	ADD CONSTRAINT fk_Right_RoleID FOREIGN KEY (RoleID) REFERENCES scada_schema.Role(RoleID);
ALTER TABLE scada_schema.Right
	ADD CONSTRAINT fk_Right_ItfID FOREIGN KEY (ItfID) REFERENCES scada_schema.Interface(ItfID);
ALTER TABLE scada_schema.User
	ADD CONSTRAINT fk_User_RoleID FOREIGN KEY (RoleID) REFERENCES scada_schema.Role(RoleID);