-- MySQL dump 10.13  Distrib 5.5.37, for Win32 (AMD64)
--
-- Host: 127.0.0.1    Database: war_world
-- ------------------------------------------------------
-- Server version	5.6.15-log

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `gameobject_loots`
--

DROP TABLE IF EXISTS `gameobject_loots`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `gameobject_loots` (
  `Entry` int(10) unsigned NOT NULL,
  `ItemId` int(10) unsigned NOT NULL,
  `Pct` float NOT NULL,
  `GameObject_loots_ID` varchar(255) NOT NULL,
  PRIMARY KEY (`Entry`,`ItemId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `gameobject_loots`
--

LOCK TABLES `gameobject_loots` WRITE;
/*!40000 ALTER TABLE `gameobject_loots` DISABLE KEYS */;
INSERT INTO `gameobject_loots` VALUES (12,434979,100,'f356ba48-f538-11e3-a79e-406c8f12b734'),
(20,17537,100,'231d38ed-f53a-11e3-a79e-406c8f12b734'),
(25,12982154,100,'874116b0-f539-11e3-a79e-406c8f12b734'),
(218,66302,100,'22f36946-0f9e-11e4-ba68-94de807e9be8'),
(488,12982153,100,'af633e5b-f538-11e3-a79e-406c8f12b734'),
(508,12982155,100,'c48c46a1-f539-11e3-a79e-406c8f12b734'),
(512,17540,100,'e6e6f114-eb50-11e3-904f-90e6baf693b4'),
(518,12982151,100,'042c8bbb-eb51-11e3-904f-90e6baf693b4'),
(527,12982150,100,'cc8e26c9-eb50-11e3-904f-90e6baf693b4'),
(552,17546,100,''),
(553,12981140,100,''),
(561,17711,100,''),
(567,12981141,100,''),
(570,12981138,100,''),
(578,12981139,100,''),
(99931,12981137,100,' ');
/*!40000 ALTER TABLE `gameobject_loots` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2014-07-27 11:03:20
