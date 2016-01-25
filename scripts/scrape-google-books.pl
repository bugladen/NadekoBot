use strict;
use warnings;
use HTML::Entities;

my $url = 'https://www.google.com/search?tbm=bks&';
my $userAgent = 'Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/47.0.2526.111 Safari/537.36';

print "\n\nPlease enter the query you would like to scrape.\n";
my $query = <STDIN>;
$query =~ s/^\s+|\s+$//g;

print "\nHow many pages?\n";
my $maxPages = <STDIN>;
$maxPages =~ s/^\s+|\s+$//g;

print "\nOutput folder?\n";
my $folder = <STDIN>;
$folder =~ s/^\s+|(\\|\/)?\s+$//g;
my $file = "$folder/$query-$maxPages.json";

my %hash;

print "\nFetching...\n";
for(my $i = 0; $i < $maxPages; $i++)
{
	my $fullUrl = $url . "q=$query";
	if($i != 0)
	{
		$fullUrl .= "&start=$i" .0;
	}
	print "Getting this: $fullUrl\n";
	my $html = `curl -A "$userAgent" -k -L "$fullUrl"`;

	#print "HTML: $html";
	#<STDIN>;
	#Disclaimer: kids, don't parse HTML like this at home
	#it's bad for your health/sanity ;)
	my @sections = split /div class="rc"/, $html;
	shift @sections;
	for my $section (@sections)
	{
		my $title;
		my $desc;
		if($section =~ /<h3\s+class="r">\s*<a\s+href="[^"]+">([^<]+)/)
		{
			$title = clean($1);
		}
		if($section =~ /<span\s+class="st">([^<]+)/)
		{
			$desc = clean($1);
		}

		print "\nTITLE:$title:\n\nDESC:$desc:\n";
		$hash{$title} = $desc;
	}

	#Google is really quick to shut down botting.
	sleep 3;
}

open FILE, ">:utf8", $file or die "Could not open file to write $!";
print FILE "[\n";
my $first = 1;
for my $title (keys %hash)
{
	if($first)
	{
		$first = 0;
	}
	else
	{
		print FILE ",\n";
	}
	next unless (defined $title and defined $hash{$title});
	next unless ($title ne '' and $hash{$title} ne '');
	print FILE "\t{\n";
	print FILE "\t\t\"Title\":\"$title\",\n";
	print FILE "\t\t\"Description\":\"$hash{$title}\"\n";
	print FILE "\t}";
}
print FILE "\n]";
close FILE;

sub clean
{
	my $str = $_[0];
	$str = decode_entities($str);
	$str =~ s/\s+\.\.\.$//;
	$str =~ s/"/'/g;
	return $str;
}