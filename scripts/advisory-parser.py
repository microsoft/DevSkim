from bs4 import BeautifulSoup
import requests
import copy
import json
import re
import logging
import sys

logger = logging.getLogger(__name__)
out_hdlr = logging.StreamHandler(sys.stderr)
out_hdlr.setFormatter(logging.Formatter('%(asctime)s %(message)s'))
out_hdlr.setLevel(logging.DEBUG)
logger.addHandler(out_hdlr)

RULE_TEMPLATE = {
	'id': None,
	'name': 'Vulnerable NuGet Library',
	"tags": [
		"Vulerable-Dependency.Library.NuGet"
    ],
    "severity": "moderate",
    "description": None,
    "replacement": "Upgrade this package to a later, unaffected version.",
    "rule_info": None,
    "applies_to": [
        "packages.config"
    ],
    "patterns": []
}

rule_number = 300000	# Starting number for rule ids

def parse_top_url(url='https://technet.microsoft.com/en-us/security/advisories'):
	logger.debug('parse_top_url({0})'.format(url))

	html = requests.get(url).text
	soup = BeautifulSoup(html, 'html5lib')

	rules = []

	div = soup.find_all('div', id='sec_advisory')[0]
	for table in div.find_all('table'):
		try:
			for row in table.find_all('tr'):
				try:
					a = row.find_all('td')[2].a
					if not a:
						continue
					href = a['href']
					result = process_advisory(href)
					if result:
						rules.append(result)

				except Exception as msg:
					logger.warn('Error parsing advisory list: {0}'.format(msg))
		except Exception as m:
			logger.warn('Error parsing advisory list: {0}'.format(m))

	logger.debug('Processing complete, outputting result.')
	print(json.dumps(rules, indent=2))

def is_correct_table(table):
	"""Check to ensure we're in an advisory details table."""
	logger.debug('is_correct_table()')

	try:
		top_row = table.find_all('tr')[0]
		first_cell = top_row.find_all('td')[0]
		text = first_cell.get_text().strip().lower()
		return 'affected' in text
	except Exception as msg:
		logger.debug('Exception checking table: {0}'.format(msg))
		return False


def process_advisory(url):
	"""Process an advisory URL."""
	global rule_number, RULE_TEMPLATE

	logger.debug('process_advisory({0})'.format(url))

	html = requests.get(url).text
	soup = BeautifulSoup(html, 'html5lib')

	rule = copy.deepcopy(RULE_TEMPLATE)
	num = 0
	found = False

	rule['description'] = soup.find_all('h2')[0].get_text()
	rule['rule_info'] = url
	rule['id'] = 'DS{0}'.format(rule_number)
	rule_number += 1

	for table in soup.find_all('table'):
		if not is_correct_table(table):
			continue
		
		for row in table.find_all('tr')[2:]:
			cells = row.find_all('td')
			package_name = cells[0].get_text().strip()
			affected_version = ','.join(cells[1].strings)

			if not ('System.' in package_name or 'Microsoft.' in package_name):
				continue

			version_regex = []
			for version in re.split(r'[, ;]+', affected_version):
				# Ignore if version is blank / empty
				if version.strip() == '':
					continue

				version_regex.append(re.escape(version.strip()))
				found = True

			version_regex = '({0})'.format('|'.join(version_regex))			
			logger.info('Added {0} {1}'.format(package_name, version_regex))

			rule['patterns'].append({
				'pattern': '<package id="{0}" version="{1}"'.format(package_name, version_regex),
				'type': 'regex'
			})

	return rule if found else False

if __name__ == '__main__':
	parse_top_url()
