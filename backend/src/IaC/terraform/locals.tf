locals {
  # If a connection string is provided, use it.
  # Otherwise, if RDS is enabled, build it from the created instance.
  # Falls back to empty string when neither is available.
  effective_database_connection_string = var.database_connection_string != "" ? var.database_connection_string : (
    var.enable_rds ? try(
      "Server=${aws_db_instance.mysql[0].address};Port=${aws_db_instance.mysql[0].port};Database=${var.mysql_db_name};User ID=${var.mysql_username};Password=${var.mysql_password};SslMode=Preferred",
      ""
    ) : ""
  )
}

