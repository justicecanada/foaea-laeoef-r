﻿using Outgoing.FileCreator.Fed.Interception;

var process = new string[] { "OASBFOUT", "TRBFOUT" };
await OutgoingFileCreatorFedInterception.RunBlockFunds(process);