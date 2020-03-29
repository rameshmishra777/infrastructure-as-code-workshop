using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Pulumi;

class MyStack : Stack
{
    public MyStack()
    {
        var ami = Output.Create(Pulumi.Aws.GetAmi.InvokeAsync(new Pulumi.Aws.GetAmiArgs
        {
            MostRecent = true,
            Owners = {"137112412989"},
            Filters = {new Pulumi.Aws.Inputs.GetAmiFiltersArgs
            {
                Name = "name", Values = {"amzn-ami-hvm-*"}
            }}
        }));
        
        var group = new Pulumi.Aws.Ec2.SecurityGroup("web-secgrp", new Pulumi.Aws.Ec2.SecurityGroupArgs
        {
            Description = "Enable HTTP access",
            Ingress =
            {
                new Pulumi.Aws.Ec2.Inputs.SecurityGroupIngressArgs
                {
                    Protocol = "tcp",
                    FromPort = 80,
                    ToPort = 80,
                    CidrBlocks = {"0.0.0.0/0"}
                },
                new Pulumi.Aws.Ec2.Inputs.SecurityGroupIngressArgs
                {
                    Protocol = "icmp",
                    FromPort = 8,
                    ToPort = 80,
                    CidrBlocks = {"0.0.0.0/0"}
                }
            }
        });
        
        var userData = @"
#!/bin/bash
echo ""Hello, World!"" > index.html
nohup python -m SimpleHTTPServer 80 &
";

        var hostNames = Pulumi.Aws.GetAvailabilityZones.InvokeAsync(new Pulumi.Aws.GetAvailabilityZonesArgs()).Result;
        var hostnames = new List<Output<string>>();
        var ips = new List<Output<string>>();
        foreach (var az in hostNames.Names)
        {
            if (az == "us-west-2d")
            {
                continue;
            }
            var server = new Pulumi.Aws.Ec2.Instance($"web-server-{az}", new Pulumi.Aws.Ec2.InstanceArgs
            {
                InstanceType = "t2.micro",
                VpcSecurityGroupIds = {group.Id},
                UserData = userData,
                Ami = ami.Apply(a => a.Id),
                AvailabilityZone = az,
            });

            hostnames.Add(server.PublicDns);
            ips.Add(server.PublicIp);
        }
    }
    
    [Output] public Output<string> PublicIps { get; set; }
    [Output] public Output<string> PublicDns { get; set; }
}
