# Issue Suppression

Like in C#, issues can be suppressed, though a little bitte different:

```sql
-- #pragma diagnostic disable AJ5000 Dynamic SQL -> Put your suppression reason here

EXEC (@cmd) -- This would yield an issue of type AJ5000 but it is suppressed

-- #pragma diagnostic restore AJ5000
```

The text after the arrow `->` will be used as suppression reason and it will be part of the report.
