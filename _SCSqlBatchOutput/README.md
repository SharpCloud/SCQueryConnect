This folder has been excluded from source control tracking but should not be
removed: the build outputs of `SCSQLBatch` and `SCSQLBatchx86` are compressed
into `.zip` files as post-build steps and will be copied to this folder. Both
`.zip` files are subsequently referenced by `SCQueryConnect` and
`SCQueryConnectx86`.